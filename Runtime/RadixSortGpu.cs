using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace H2o.Sort
{
  public sealed class RadixSortGpu : System.IDisposable
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
    static readonly int EntriesId = Shader.PropertyToID("_Entries");
    static readonly int SortedEntriesId = Shader.PropertyToID("_SortedEntries");


    private int _kernelClearGlobalHistogram = -1;
    private int _kernelCount = -1;
    private int _kernelScan = -1;
    private int _kernelReorder = -1;


    private LocalKeyword _countEnablePayload;
    private LocalKeyword _reorderEnablePayload;

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
    public RadixSortGpu(uint keyCapacity, RadixSortGpuSettings settings)
    {
      settings.AssertVald();

      _radixCount = settings.RadixCount;
      _radixScan = settings.RadixScan;
      _radixReorder = settings.RadixReorder;

      _countEnablePayload = new LocalKeyword(_radixCount, "ENABLE_PAYLOAD");
      _reorderEnablePayload = new LocalKeyword(_radixReorder, "ENABLE_PAYLOAD");

      _globalHistogram = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)RadixUtils.GlobalBinCount, sizeof(uint));

      FetchKernelIds();

      _radixScan.GetKernelThreadGroupSizes(_kernelScan, out uint groupSize, out _, out _);
      // _BlockHistogram must be aligned to groupSize
      uint blockCount = GetBlockCount(keyCapacity);
      uint maxBlockCapacity = RadixUtils.CeilDiv(blockCount, groupSize) * groupSize;
      uint blockHistogramSize = RadixUtils.GetBlockHistogramSize(maxBlockCapacity);
      _blockHistogram = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)blockHistogramSize, sizeof(uint));
      _blockHistogramT = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)blockHistogramSize, sizeof(uint));
      _radixParams = new ConstantBuffer<RadixParams>();
      _maxKeyCapacity = GetKeyCount(maxBlockCapacity);
    }
    ~RadixSortGpu()
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
    public GraphicsBuffer Dispatch(CommandBuffer cmd, RadixSortGpuParams rsParams)
    {
      rsParams.AssertValid();
      Assert.IsNotNull(cmd, $"{nameof(Dispatch)}: {nameof(cmd)} is null");
      Assert.IsTrue(_maxKeyCapacity >= rsParams.EntryCount, $"{nameof(_maxKeyCapacity)}({_maxKeyCapacity}) < {nameof(rsParams)}.{nameof(rsParams.EntryCount)}({rsParams.EntryCount})");

      int passCount = RadixUtils.GetPassCount(rsParams.MaxKey);
      uint globalBinCount = RadixUtils.GetGlobalBinCount(passCount);
      uint blockCount = GetBlockCount(rsParams.EntryCount);

      cmd.SetKeyword(_radixCount, _countEnablePayload, rsParams.EnablePayload);
      cmd.SetKeyword(_radixReorder, _reorderEnablePayload, rsParams.EnablePayload);

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
        EntryCount = rsParams.EntryCount,
        BlockCount = blockCount
      };

      GraphicsBuffer source = rsParams.Entries;
      GraphicsBuffer destination = rsParams.TempEntries;
      for (uint passIndex = 0; passIndex < passCount; ++passIndex)
      {
        radixParams.PassIndex = passIndex;
        _radixParams.UpdateData(cmd, radixParams);

        cmd.SetComputeBufferParam(_radixCount, _kernelCount, EntriesId, source);

        cmd.SetComputeBufferParam(_radixReorder, _kernelReorder, EntriesId, source);
        cmd.SetComputeBufferParam(_radixReorder, _kernelReorder, SortedEntriesId, destination);

        cmd.BeginSample("RadixCount");
        cmd.DispatchCompute(_radixCount, _kernelCount, (int)blockCount, 1, 1);
        cmd.EndSample("RadixCount");

        cmd.BeginSample("RadixScan");
        cmd.DispatchCompute(_radixScan, _kernelScan, RadixUtils.BinCount, 1, 1);
        cmd.EndSample("RadixScan");

        cmd.BeginSample("RadixReorder");
        cmd.DispatchCompute(_radixReorder, _kernelReorder, (int)blockCount, 1, 1);
        cmd.EndSample("RadixReorder");
        (source, destination) = (destination, source);
      }
      return source;
    }
    void FetchKernelIds()
    {
      _kernelClearGlobalHistogram = _radixCount.FindKernel("RadixClearGlobalHistogram");
      _kernelCount = _radixCount.FindKernel("RadixCount");
      _kernelScan = _radixScan.FindKernel("RadixScan");
      _kernelReorder = _radixReorder.FindKernel("RadixReorder");
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
      [FieldOffset(0)] public uint PassIndex;
      [FieldOffset(4)] public uint EntryCount;
      [FieldOffset(8)] public uint BlockCount;
    }
  }
}
