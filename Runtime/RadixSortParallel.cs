using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace H2o.Sort
{
  public class RadixSortParallel
  {
    [BurstCompile]
    public struct RadixClearHistogramJob : IJob
    {
      [WriteOnly]
      public NativeArray<uint> histogram;
      public void Execute()
      {
        histogram.ClearMemory();
      }
    }

    [BurstCompile]
    public struct RadixCountJob : IJobFor
    {
      public int offsetBits;
      public int keyCount;
      public int keyBlockSize;
      [ReadOnly] public NativeArray<uint> keys;
      [NativeDisableParallelForRestriction]
      public NativeArray<int> histogram;
      public void Execute(int blockIndex)
      {
        int blockHistogramStart = blockIndex * RadixUtils.BinCount;
        int blockHistogramEnd = blockHistogramStart + RadixUtils.BinCount;
        int keyBlockStart = blockIndex * keyBlockSize;
        int keyBlockEnd = math.min(keyBlockStart + keyBlockSize, keyCount);
        for (int i = keyBlockStart; i < keyBlockEnd; i++)
        {
          uint val = keys[i];
          int index = (int)((val >> offsetBits) & RadixUtils.Mask);
          index += blockHistogramStart;
          histogram[index]++;
        }
      }
    }
    [BurstCompile]
    public struct RadixScanJob : IJob
    {
      public int batchCount;
      public NativeArray<uint> historgram;
      public void Execute()
      {
        uint sum = 0;
        for (var i = 0; i < RadixUtils.BinCount; ++i)
        {
          for (var j = 0; j < batchCount; ++j)
          {
            int index = j * RadixUtils.BinCount + i;
            uint count = historgram[index];
            historgram[index] = sum;
            sum += count;
          }
        }
      }
    }
    [BurstCompile]
    public struct RadixReorderJob : IJobFor
    {
      public int offsetBits;
      public int keyCount;
      public int keyBlockSize;

      [ReadOnly]
      public NativeArray<uint> keys;
      public NativeArray<uint> payloads;
      [NativeDisableParallelForRestriction]
      public NativeArray<int> histogram;
      [WriteOnly]
      [NativeDisableParallelForRestriction]
      public NativeArray<uint> sortedKeys;
      [WriteOnly]
      [NativeDisableParallelForRestriction]
      public NativeArray<uint> sortedPayloads;
      public void Execute(int blockIndex)
      {
        int blockHistogramStart = blockIndex * RadixUtils.BinCount;
        int keyBlockStart = blockIndex * keyBlockSize;
        int keyBlockEnd = math.min(keyBlockStart + keyBlockSize, keyCount);

        for (int i = keyBlockStart; i < keyBlockEnd; i++)
        {
          uint key = keys[i];
          int bin = (int)((key >> offsetBits) & RadixUtils.Mask);
          int counterIndex = blockHistogramStart + bin;
          int targetIndex = histogram[counterIndex];
          sortedKeys[targetIndex] = key;
          sortedPayloads[targetIndex] = payloads[i];
          targetIndex++;
          histogram[counterIndex] = targetIndex;
        }
      }
    }
  }
}
