using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace H2o.Sort
{
  public partial class RadixSort<TEntry>
  {
    public sealed class Parallel : System.IDisposable, IRadixSort<TEntry>
    {
      NativeArray<int> _blockHistogram;

      int _maxBlockCount;
      private bool _disposed = false;
      public Parallel()
      {
        _maxBlockCount = (JobsUtility.JobWorkerCount + 1) * 4;
        _blockHistogram = new NativeArray<int>(_maxBlockCount * RadixUtils.BinCount, Allocator.Persistent);
      }
      ~Parallel()
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
        if (_blockHistogram.IsCreated) _blockHistogram.Dispose();

        _disposed = true;
      }
      int GetBlockSize(int count)
      {
        return math.max(RadixUtils.MinBlockSize, RadixUtils.CeilDiv(count, _maxBlockCount));
      }
      public JobHandle Schedule(RadixSortParams<TEntry> rsParams, out NativeArray<TEntry> sortedEntries)
      {
        int passCount = rsParams.PassCount;
        int blockSize = GetBlockSize(rsParams.EntryCount);
        int blockCount = RadixUtils.CeilDiv(rsParams.EntryCount, blockSize);

        sortedEntries = ((passCount & 1) == 0) ? rsParams.Entries : rsParams.TempEntries;

        NativeArray<TEntry> srcEntries = rsParams.Entries;
        NativeArray<TEntry> dstEntries = rsParams.TempEntries;

        JobHandle jobHandle = rsParams.Dependency;
        for (int passIndex = 0; passIndex < passCount; ++passIndex)
        {
          int offsetBits = passIndex * RadixUtils.BitsPerPass;
          jobHandle = new RadixCountJob()
          {
            OffsetBits = offsetBits,
            Count = rsParams.EntryCount,
            BlockSize = blockSize,
            Entries = srcEntries,
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
            Count = rsParams.EntryCount,
            BlockSize = blockSize,
            BlockHistogram = _blockHistogram,
            Entries = srcEntries,
            SortedEntries = dstEntries,
          }.ScheduleParallel(blockCount, 1, jobHandle);
          (srcEntries, dstEntries) = (dstEntries, srcEntries);
        }
        return jobHandle;
      }
      [BurstCompile]
      public struct RadixCountJob : IJobFor
      {
        public int OffsetBits;
        public int Count;
        public int BlockSize;
        [ReadOnly] public NativeArray<TEntry> Entries;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> BlockHistogram;
        public void Execute(int blockIndex)
        {
          Span<int> histogram = stackalloc int[RadixUtils.BinCount];
          int keyBlockStart = blockIndex * BlockSize;
          int keyBlockEnd = math.min(keyBlockStart + BlockSize, Count);
          for (int i = keyBlockStart; i < keyBlockEnd; i++)
          {
            uint val = Entries[i].Key;
            int index = (int)((val >> OffsetBits) & RadixUtils.Mask);
            histogram[index]++;
          }

          int blockHistogramStart = blockIndex * RadixUtils.BinCount;
          histogram.CopyTo(BlockHistogram.AsSpan().Slice(blockHistogramStart, RadixUtils.BinCount));
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

        [ReadOnly] public NativeArray<TEntry> Entries;

        [NativeDisableParallelForRestriction]
        public NativeArray<int> BlockHistogram;

        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<TEntry> SortedEntries;

        public void Execute(int blockIndex)
        {
          int blockHistogramStart = blockIndex * RadixUtils.BinCount;
          int keyBlockStart = blockIndex * BlockSize;
          int keyBlockEnd = math.min(keyBlockStart + BlockSize, Count);

          for (int i = keyBlockStart; i < keyBlockEnd; i++)
          {
            TEntry entry = Entries[i];
            uint key = entry.Key;
            int bin = (int)((key >> OffsetBits) & RadixUtils.Mask);
            int counterIndex = blockHistogramStart + bin;
            int targetIndex = BlockHistogram[counterIndex];
            SortedEntries[targetIndex] = entry;
            targetIndex++;
            BlockHistogram[counterIndex] = targetIndex;
          }
        }
      }
    }
  }

}
