using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace H2o.Sort
{
  public sealed class RadixGpuSorterV2 : System.IDisposable
  {
    // buffers
    static readonly int RadixParamsId = Shader.PropertyToID("_RadixParams");
    static readonly int GlobalHistogramId = Shader.PropertyToID("_GlobalHistogram");
    static readonly int BlockHistogramsId = Shader.PropertyToID("_BlockHistogram");
    static readonly int BlockHistogramsTId = Shader.PropertyToID("_BlockHistogramT");
    static readonly int KeysId = Shader.PropertyToID("_Keys");
    static readonly int PayloadsId = Shader.PropertyToID("_Payloads");
    static readonly int SortedPayloadsId = Shader.PropertyToID("_SortedPayloads");
    static readonly int SortedKeysId = Shader.PropertyToID("_SortedKeys");

    private int _kernelClearGlobalHistogram = -1;
    private int _kernelCount = -1;
    private int _kernelScan = -1;
    private int _kernelReorder = -1;


    private bool _disposed = false;
    private uint _maxKeyCapacity;

    private GraphicsBuffer _globalHistogram; // exclusive prefix sum
    private GraphicsBuffer _blockHistogram; // exclusive prefix sum
    private GraphicsBuffer _blockHistogramT; // count
    private ConstantBuffer<RadixParams> _radixParams;
    private ComputeShader _radixCount;
    private ComputeShader _radixScan;
    private ComputeShader _radixReorder;

    public GraphicsBuffer GlobalHistogram => _globalHistogram;
    public GraphicsBuffer BlockHistogram => _blockHistogram;
    public GraphicsBuffer BlockHistogramT => _blockHistogramT;
    public uint MaxKeyCount => _maxKeyCapacity;
    public RadixGpuSorterV2(uint maxKeyCapacity, RadixGpuSorterSettings settings)
    {
      settings.AssertVald();

      _radixCount = settings.RadixCount;
      _radixScan = settings.RadixScanDx12;
      _radixReorder = settings.RadixReorder;

      _globalHistogram = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)RadixUtils.GlobalBinCount, sizeof(uint));

      FetchKernelIds();

      _radixScan.GetKernelThreadGroupSizes(_kernelScan, out uint groupSize, out _, out _);
      // _BlockHistogram must be aligned to groupSize
      uint blockCount = GetBlockCount(maxKeyCapacity);
      uint maxBlockCapacity = RadixUtils.CeilDiv(blockCount, groupSize) * groupSize;
      uint blockHistogramSize = RadixUtils.GetBlockHistogramSize(maxBlockCapacity);
      _blockHistogram = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)blockHistogramSize, sizeof(uint));
      _blockHistogramT = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)blockHistogramSize, sizeof(uint));
      _radixParams = new ConstantBuffer<RadixParams>();
      _maxKeyCapacity = GetKeyCount(maxBlockCapacity);
    }
    ~RadixGpuSorterV2()
    {
      Dispose(false);
    }
    public void Dispose()
    {
      Dispose(true);
      System.GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
      if (_disposed) return;

      if (disposing)
      {
        // Dispose managed state (managed objects).

      }

      // Free unmanaged resources.

      ComputeHelpers.ReleaseBuffer(ref _globalHistogram);
      ComputeHelpers.ReleaseBuffer(ref _blockHistogram);
      ComputeHelpers.ReleaseBuffer(ref _blockHistogramT);
      ComputeHelpers.ReleaseBuffer(ref _radixParams);

      _disposed = true;
    }
    uint GetBlockCount(uint keyCount)
    {
      return RadixUtils.CeilDiv(keyCount, RadixUtils.BlockSize);
    }
    uint GetKeyCount(uint blockCount)
    {
      return blockCount * RadixUtils.BlockSize;
    }
    /// <summary>
    /// Executes the GPU Radix Sort.
    /// </summary>
    /// <returns>
    /// The <see cref="EntryGraphicsBuffers"/> containing the final sorted result.
    /// <para>Note: Due to ping-pong swapping, this may be either the input Entries or TempEntries.</para>
    /// </returns>
    public EntryGraphicsBuffers Dispatch(CommandBuffer cmd, RadixGpuSorterParams rsParams)
    {
      rsParams.AssertValid();
      Assert.IsNotNull(cmd, $"{nameof(Dispatch)}: {nameof(cmd)} is null");
      Assert.IsTrue(_maxKeyCapacity >= rsParams.KeyCount, $"{nameof(_maxKeyCapacity)}({_maxKeyCapacity}) < {nameof(rsParams)}.{nameof(rsParams.KeyCount)}({rsParams.KeyCount})");

      int passCount = RadixUtils.GetPassCount(rsParams.MaxKey);
      uint globalBinCount = RadixUtils.GetGlobalBinCount(passCount);
      uint blockCount = GetBlockCount(rsParams.KeyCount);
      //Debug.Log($"{nameof(passCount)} = {passCount}");
      //Debug.Log($"{nameof(globalBinCount)} = {globalBinCount}");
      //Debug.Log($"{nameof(blockCount)} = {blockCount}");
      //Debug.Log($"{nameof(RadixUtils.BlockSize)} = {RadixUtils.BlockSize}");

      cmd.SetComputeBufferParam(_radixCount, _kernelClearGlobalHistogram, GlobalHistogramId, _globalHistogram);
      ComputeHelpers.Dispatch(cmd, _radixCount, _kernelClearGlobalHistogram, globalBinCount);


      cmd.SetComputeBufferParam(_radixCount, _kernelCount, GlobalHistogramId, _globalHistogram);
      cmd.SetComputeBufferParam(_radixCount, _kernelCount, BlockHistogramsTId, _blockHistogramT);


      cmd.SetComputeBufferParam(_radixScan, _kernelScan, GlobalHistogramId, _globalHistogram);
      cmd.SetComputeBufferParam(_radixScan, _kernelScan, BlockHistogramsTId, _blockHistogramT);
      cmd.SetComputeBufferParam(_radixScan, _kernelScan, BlockHistogramsId, _blockHistogram);


      cmd.SetComputeBufferParam(_radixReorder, _kernelReorder, BlockHistogramsId, _blockHistogram);

      _radixParams.Set(cmd, _radixCount, RadixParamsId);
      _radixParams.Set(cmd, _radixScan, RadixParamsId);
      _radixParams.Set(cmd, _radixReorder, RadixParamsId);


      RadixParams radixParams = new RadixParams
      {
        keyCount = rsParams.KeyCount,
        blockCount = blockCount
      };

      EntryGraphicsBuffers source = rsParams.Entries;
      EntryGraphicsBuffers destination = rsParams.TempEntries;
      for (uint passIndex = 0; passIndex < passCount; ++passIndex)
      {
        radixParams.passIndex = passIndex;
        _radixParams.UpdateData(cmd, radixParams);

        cmd.SetComputeBufferParam(_radixCount, _kernelCount, KeysId, source.Keys);

        cmd.SetComputeBufferParam(_radixReorder, _kernelReorder, KeysId, source.Keys);
        cmd.SetComputeBufferParam(_radixReorder, _kernelReorder, PayloadsId, source.Payloads);
        cmd.SetComputeBufferParam(_radixReorder, _kernelReorder, SortedKeysId, destination.Keys);
        cmd.SetComputeBufferParam(_radixReorder, _kernelReorder, SortedPayloadsId, destination.Payloads);

        cmd.BeginSample("RadixCountV3");
        cmd.DispatchCompute(_radixCount, _kernelCount, (int)blockCount, 1, 1);
        cmd.EndSample("RadixCountV3");

        cmd.BeginSample("RadixScan");
        cmd.DispatchCompute(_radixScan, _kernelScan, RadixUtils.BinCount, 1, 1);
        cmd.EndSample("RadixScan");

        cmd.BeginSample("RadixReorderV3");
        cmd.DispatchCompute(_radixReorder, _kernelReorder, (int)blockCount, 1, 1);
        cmd.EndSample("RadixReorderV3");
        (source, destination) = (destination, source);
      }
      return source;
    }
    public EntryGraphicsBuffers Dispatch(RadixGpuSorterParams rsParams)
    {
      rsParams.AssertValid();
      Assert.IsTrue(_maxKeyCapacity >= rsParams.KeyCount, $"{nameof(_maxKeyCapacity)}({_maxKeyCapacity}) < {nameof(rsParams)}.{nameof(rsParams.KeyCount)}({rsParams.KeyCount})");

      int passCount = RadixUtils.GetPassCount(rsParams.MaxKey);
      uint globalBinCount = RadixUtils.GetGlobalBinCount(passCount);
      uint blockCount = GetBlockCount(rsParams.KeyCount);
      Debug.Log($"{nameof(passCount)} = {passCount}");
      Debug.Log($"{nameof(globalBinCount)} = {globalBinCount}");
      Debug.Log($"{nameof(blockCount)} = {blockCount}");

      _radixCount.SetBuffer(_kernelClearGlobalHistogram, GlobalHistogramId, _globalHistogram);
      ComputeHelpers.Dispatch(_radixCount, _kernelClearGlobalHistogram, globalBinCount);


      _radixCount.SetBuffer(_kernelCount, GlobalHistogramId, _globalHistogram);
      _radixCount.SetBuffer(_kernelCount, BlockHistogramsTId, _blockHistogramT);


      _radixScan.SetBuffer(_kernelScan, GlobalHistogramId, _globalHistogram);
      _radixScan.SetBuffer(_kernelScan, BlockHistogramsTId, _blockHistogramT);
      _radixScan.SetBuffer(_kernelScan, BlockHistogramsId, _blockHistogram);

      _radixReorder.SetBuffer(_kernelReorder, BlockHistogramsId, _blockHistogram);

      _radixParams.Set(_radixCount, RadixParamsId);
      _radixParams.Set(_radixScan, RadixParamsId);
      _radixParams.Set(_radixReorder, RadixParamsId);

      RadixParams radixParams = new RadixParams
      {
        keyCount = rsParams.KeyCount,
        blockCount = blockCount
      };

      EntryGraphicsBuffers source = rsParams.Entries;
      EntryGraphicsBuffers destination = rsParams.TempEntries;
      for (uint passIndex = 0; passIndex < passCount; ++passIndex)
      {
        radixParams.passIndex = passIndex;
        _radixParams.UpdateData(radixParams);

        _radixCount.SetBuffer(_kernelCount, KeysId, source.Keys);

        _radixReorder.SetBuffer(_kernelReorder, KeysId, source.Keys);
        _radixReorder.SetBuffer(_kernelReorder, PayloadsId, source.Payloads);
        _radixReorder.SetBuffer(_kernelReorder, SortedKeysId, destination.Keys);
        _radixReorder.SetBuffer(_kernelReorder, SortedPayloadsId, destination.Payloads);

        _radixCount.Dispatch(_kernelCount, (int)blockCount, 1, 1);

        _radixScan.Dispatch(_kernelScan, RadixUtils.BinCount, 1, 1);
        _radixReorder.Dispatch(_kernelReorder, (int)blockCount, 1, 1);
        (source, destination) = (destination, source);
      }
      return source;
    }
    void FetchKernelIds()
    {
      _kernelClearGlobalHistogram = _radixCount.FindKernel("RadixClearGlobalHistogram");
      _kernelCount = _radixCount.FindKernel("RadixCountV3");
      _kernelScan = _radixScan.FindKernel("RadixScan");
      _kernelReorder = _radixReorder.FindKernel("RadixReorderV3");
    }
    // --- std140 Layout Alignment Rules ---
    // 1. Scalar (int, float, uint, bool): 
    //    Align by 4 bytes. Offset must be multiple of 4.
    // 2. Vector2 (vec2): 
    //    Align by 8 bytes. Offset must be multiple of 8.
    // 3. Vector3 / Vector4 (vec3, vec4): 
    //    Align by 16 bytes. Offset must be multiple of 16.
    // 4. Array (any type): 
    //    Align by 16 bytes PER ELEMENT. Each element pads to 16.
    // 5. Struct: 
    //    Align by 16 bytes. Base offset must be multiple of 16.
    // 6. Matrix (mat4): 
    //    Treated as 4 separate vec4s. Total 64 bytes.
    // 7. Total Struct Size (Size Property): 
    //    MUST be aligned to a multiple of 16 bytes (16, 32, 48, 64, etc.).
    // -------------------------------------------------------

    [StructLayout(LayoutKind.Explicit, Size = 16)] // Aligned to 16 bytes
    struct RadixParams
    {
      [FieldOffset(0)] public uint passIndex;
      [FieldOffset(4)] public uint keyCount;
      [FieldOffset(8)] public uint blockCount;
    }
  }
}
