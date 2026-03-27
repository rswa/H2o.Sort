using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace H2o.Sort
{
  public sealed class RadixSortGpu : System.IDisposable
  {
    private bool _disposed = false;
    private uint _entryCapacity;

    private GraphicsBuffer _globalHistogram; // exclusive prefix sum
    private GraphicsBuffer _blockHistogram; // exclusive prefix sum
    private GraphicsBuffer _blockHistogramT; // count
    private ConstantBuffer<RadixParams> _radixParams;

    private ComputeShader _countShader;
    private ComputeShader _scanShader;
    private ComputeShader _reorderShader;

    private Kernels _kernels;
    private LocalKeywords _localKeywords;
    public GraphicsBuffer GlobalHistogram => _globalHistogram;
    public GraphicsBuffer BlockHistogram => _blockHistogram;
    public GraphicsBuffer BlockHistogramT => _blockHistogramT;
    public uint EntryCapcaity => _entryCapacity;
    public RadixSortGpu(uint entryCapacity, RadixSortGpuSettings settings)
    {
      settings.AssertValid();

      _countShader = settings.RadixCount;
      _scanShader = settings.RadixScan;
      _reorderShader = settings.RadixReorder;

      _kernels = new Kernels(_countShader, _scanShader, _reorderShader);
      _localKeywords = new LocalKeywords(_countShader, _scanShader, _reorderShader);

      _scanShader.GetKernelThreadGroupSizes(_kernels.RadixScan, out uint groupSize, out _, out _);
      // _BlockHistogram must be aligned to groupSize
      uint blockCount = RadixUtils.GetGpuBlockCount(entryCapacity);
      uint maxBlockCapacity = RadixUtils.CeilDiv(blockCount, groupSize) * groupSize;
      uint blockHistogramSize = RadixUtils.GetBlockHistogramSize(maxBlockCapacity);
      _entryCapacity = RadixUtils.GetGpuEntryCount(maxBlockCapacity);

      _blockHistogram = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)blockHistogramSize, sizeof(uint));
      _blockHistogramT = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)blockHistogramSize, sizeof(uint));
      _radixParams = new ConstantBuffer<RadixParams>();
      _globalHistogram = new GraphicsBuffer(GraphicsBuffer.Target.Structured, RadixUtils.GlobalBinCount, sizeof(uint));
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
    /// <summary>
    /// Executes the GPU Radix Sort.
    /// </summary>
    /// <returns>
    /// <para>Note: Due to ping-pong swapping, this may be either the input Entries or TempEntries.</para>
    /// </returns>
    public GraphicsBuffer Dispatch(CommandBuffer cmd, RadixSortGpuParams rsParams)
    {
      rsParams.AssertValid();
      Assert.IsNotNull(cmd, $"[{nameof(RadixSortGpu)}.{nameof(Dispatch)}] {nameof(cmd)} is null.");
      Assert.IsTrue(_entryCapacity >= rsParams.EntryCount, $"{nameof(_entryCapacity)}({_entryCapacity}) < {nameof(rsParams)}.{nameof(rsParams.EntryCount)}({rsParams.EntryCount})");

      int passCount = RadixUtils.GetPassCount(rsParams.MaxKey);
      uint globalBinCount = RadixUtils.GetGlobalBinCount(passCount);
      uint blockCount = RadixUtils.GetGpuBlockCount(rsParams.EntryCount);

      cmd.SetKeyword(_countShader, _localKeywords.CountEnablePayload, rsParams.EnablePayload);
      cmd.SetKeyword(_reorderShader, _localKeywords.ReorderEnablePayload, rsParams.EnablePayload);

      cmd.SetComputeBufferParam(_countShader, _kernels.RadixClearGlobalHistogram, ShaderIds._GlobalHistogram, _globalHistogram);
      ComputeHelpers.Dispatch(cmd, _countShader, _kernels.RadixClearGlobalHistogram, globalBinCount);


      cmd.SetComputeBufferParam(_countShader, _kernels.RadixCount, ShaderIds._GlobalHistogram, _globalHistogram);
      cmd.SetComputeBufferParam(_countShader, _kernels.RadixCount, ShaderIds._BlockHistogramT, _blockHistogramT);


      cmd.SetComputeBufferParam(_scanShader, _kernels.RadixScan, ShaderIds._GlobalHistogram, _globalHistogram);
      cmd.SetComputeBufferParam(_scanShader, _kernels.RadixScan, ShaderIds._BlockHistogramT, _blockHistogramT);
      cmd.SetComputeBufferParam(_scanShader, _kernels.RadixScan, ShaderIds._BlockHistogram, _blockHistogram);


      cmd.SetComputeBufferParam(_reorderShader, _kernels.RadixReorder, ShaderIds._BlockHistogram, _blockHistogram);

      _radixParams.Set(cmd, _countShader, ShaderIds._RadixParams);
      _radixParams.Set(cmd, _scanShader, ShaderIds._RadixParams);
      _radixParams.Set(cmd, _reorderShader, ShaderIds._RadixParams);


      RadixParams radixParams = new RadixParams
      {
        EntryCount = rsParams.EntryCount,
        BlockCount = blockCount
      };

      GraphicsBuffer entities = rsParams.Entries;
      GraphicsBuffer tempEntities = rsParams.TempEntries;
      for (uint passIndex = 0; passIndex < passCount; ++passIndex)
      {
        radixParams.PassIndex = passIndex;
        _radixParams.UpdateData(cmd, radixParams);

        cmd.SetComputeBufferParam(_countShader, _kernels.RadixCount, ShaderIds._Entries, entities);

        cmd.SetComputeBufferParam(_reorderShader, _kernels.RadixReorder, ShaderIds._Entries, entities);
        cmd.SetComputeBufferParam(_reorderShader, _kernels.RadixReorder, ShaderIds._SortedEntries, tempEntities);

        cmd.BeginSample(nameof(_kernels.RadixCount));
        cmd.DispatchCompute(_countShader, _kernels.RadixCount, (int)blockCount, 1, 1);
        cmd.EndSample(nameof(_kernels.RadixCount));

        cmd.BeginSample(nameof(_kernels.RadixScan));
        cmd.DispatchCompute(_scanShader, _kernels.RadixScan, RadixUtils.BinCount, 1, 1);
        cmd.EndSample(nameof(_kernels.RadixScan));

        cmd.BeginSample(nameof(_kernels.RadixReorder));
        cmd.DispatchCompute(_reorderShader, _kernels.RadixReorder, (int)blockCount, 1, 1);
        cmd.EndSample(nameof(_kernels.RadixReorder));
        (entities, tempEntities) = (tempEntities, entities);
      }
      return entities;
    }
    static class ShaderIds
    {
      public static readonly int _RadixParams = Shader.PropertyToID(nameof(_RadixParams));
      public static readonly int _GlobalHistogram = Shader.PropertyToID(nameof(_GlobalHistogram));
      public static readonly int _BlockHistogram = Shader.PropertyToID(nameof(_BlockHistogram));
      public static readonly int _BlockHistogramT = Shader.PropertyToID(nameof(_BlockHistogramT));
      public static readonly int _Entries = Shader.PropertyToID(nameof(_Entries));
      public static readonly int _SortedEntries = Shader.PropertyToID(nameof(_SortedEntries));
    }
    struct Kernels
    {
      public readonly int RadixClearGlobalHistogram;
      public readonly int RadixCount;
      public readonly int RadixScan;
      public readonly int RadixReorder;
      public Kernels(ComputeShader countShader, ComputeShader scanShader, ComputeShader reorderShader)
      {
        RadixClearGlobalHistogram = countShader.FindKernel(nameof(RadixClearGlobalHistogram));
        RadixCount = countShader.FindKernel(nameof(RadixCount));
        RadixScan = scanShader.FindKernel(nameof(RadixScan));
        RadixReorder = reorderShader.FindKernel(nameof(RadixReorder));
      }
    }
    struct LocalKeywords
    {
      public readonly LocalKeyword CountEnablePayload;
      public readonly LocalKeyword ReorderEnablePayload;
      public LocalKeywords(ComputeShader countShader, ComputeShader scanShader, ComputeShader reorderShader)
      {
        CountEnablePayload = new LocalKeyword(countShader, "ENABLE_PAYLOAD");
        ReorderEnablePayload = new LocalKeyword(reorderShader, "ENABLE_PAYLOAD");
      }
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
