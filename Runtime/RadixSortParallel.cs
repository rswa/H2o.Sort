using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace H2o.Sort
{
  public sealed class RadixSortParallel : System.IDisposable
  {
    NativeArray<int> _blockHistogram;

    int _maxBlockCount;
    private bool _disposed = false;
    public RadixSortParallel()
    {
      _maxBlockCount = JobsUtility.JobWorkerCount + 1;
      _blockHistogram = new NativeArray<int>((_maxBlockCount * RadixUtils.BinCount), Allocator.Persistent);
    }
    ~RadixSortParallel()
    {
      Dispose(false);
    }

    int GetBlockSize(int count)
    {
      return math.max(RadixUtils.MinBlockSize, RadixUtils.CeilDiv(count, _maxBlockCount));
    }
    public JobHandle Schedule(RadixSortParams rsParams, out NativeEntries sortedData)
    {
      int passCount = rsParams.PassCount;
      int blockSize = GetBlockSize(rsParams.Count);
      int blockCount = RadixUtils.CeilDiv(rsParams.Count, blockSize);

      sortedData = ((passCount & 1) == 0) ? rsParams.Entries : rsParams.TempEntries;

      NativeEntries srcEntries = rsParams.Entries;
      NativeEntries dstEntries = rsParams.TempEntries;

      JobHandle jobHandle = rsParams.Dependency;
      for (int passIndex = 0; passIndex < passCount; ++passIndex)
      {
        int offsetBits = passIndex * RadixUtils.BitsPerPass;
        jobHandle = new RadixClearHistogramJob()
        {
          BlockHistogram = _blockHistogram
        }.Schedule(jobHandle);
        jobHandle = new RadixCountJob()
        {
          OffsetBits = offsetBits,
          Count = rsParams.Count,
          KeyBlockSize = blockSize,
          Keys = srcEntries.Keys,
          BlockHistogram = _blockHistogram
        }.ScheduleParallel(blockCount, 1, jobHandle);

        jobHandle = new RadixScanJob()
        {
          BlockCount = blockCount,
          BlockHistogram = _blockHistogram,
        }.Schedule(jobHandle);

        jobHandle = new RadixReorderJob()
        {
          OffsetBits = offsetBits,
          Count = rsParams.Count,
          BlockSize = blockSize,
          BlockHistogram = _blockHistogram,
          Keys = srcEntries.Keys,
          Payloads = srcEntries.Payloads,
          SortedKeys = dstEntries.Keys,
          SortedPayloads = dstEntries.Payloads,
        }.ScheduleParallel(blockCount, 1, jobHandle);
        (srcEntries, dstEntries) = (dstEntries, srcEntries);
      }
      return jobHandle;
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
      if (_blockHistogram.IsCreated) _blockHistogram.Dispose();

      _disposed = true;
    }

    [BurstCompile]
    public struct RadixClearHistogramJob : IJob
    {
      [WriteOnly]
      public NativeArray<int> BlockHistogram;
      public void Execute()
      {
        BlockHistogram.ClearMemory();
      }
    }

    [BurstCompile]
    public struct RadixCountJob : IJobFor
    {
      public int OffsetBits;
      public int Count;
      public int KeyBlockSize;
      [ReadOnly] public NativeArray<uint> Keys;
      [NativeDisableParallelForRestriction]
      public NativeArray<int> BlockHistogram;
      public void Execute(int blockIndex)
      {
        int blockHistogramStart = blockIndex * RadixUtils.BinCount;
        int blockHistogramEnd = blockHistogramStart + RadixUtils.BinCount;
        int keyBlockStart = blockIndex * KeyBlockSize;
        int keyBlockEnd = math.min(keyBlockStart + KeyBlockSize, Count);
        for (int i = keyBlockStart; i < keyBlockEnd; i++)
        {
          uint val = Keys[i];
          int index = (int)((val >> OffsetBits) & RadixUtils.Mask);
          index += blockHistogramStart;
          BlockHistogram[index]++;
        }
      }
    }
    [BurstCompile]
    public struct RadixScanJob : IJob
    {
      public int BlockCount;
      public NativeArray<int> BlockHistogram;
      public void Execute()
      {
        int sum = 0;
        for (var i = 0; i < RadixUtils.BinCount; ++i)
        {
          for (var j = 0; j < BlockCount; ++j)
          {
            int index = j * RadixUtils.BinCount + i;
            int count = BlockHistogram[index];
            BlockHistogram[index] = sum;
            sum += count;
          }
        }
      }
    }
    [BurstCompile]
    public struct RadixReorderJob : IJobFor
    {
      public int OffsetBits;
      public int Count;
      public int BlockSize;

      [ReadOnly] public NativeArray<uint> Keys;
      [ReadOnly] public NativeArray<uint> Payloads;

      [NativeDisableParallelForRestriction]
      public NativeArray<int> BlockHistogram;

      [NativeDisableParallelForRestriction]
      [WriteOnly] public NativeArray<uint> SortedKeys;

      [NativeDisableParallelForRestriction]
      [WriteOnly] public NativeArray<uint> SortedPayloads;
      public void Execute(int blockIndex)
      {
        int blockHistogramStart = blockIndex * RadixUtils.BinCount;
        int keyBlockStart = blockIndex * BlockSize;
        int keyBlockEnd = math.min(keyBlockStart + BlockSize, Count);

        for (int i = keyBlockStart; i < keyBlockEnd; i++)
        {
          uint key = Keys[i];
          int bin = (int)((key >> OffsetBits) & RadixUtils.Mask);
          int counterIndex = blockHistogramStart + bin;
          int targetIndex = BlockHistogram[counterIndex];
          SortedKeys[targetIndex] = key;
          SortedPayloads[targetIndex] = Payloads[i];
          targetIndex++;
          BlockHistogram[counterIndex] = targetIndex;
        }
      }
    }
  }
}
