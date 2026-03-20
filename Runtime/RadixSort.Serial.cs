using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace H2o.Sort
{
  public partial class RadixSort<TEntry> where TEntry : unmanaged, IEntry
  {
    public sealed class Serial : IRadixSort<TEntry>
    {
      public JobHandle Schedule(RadixSortParams<TEntry> rsParams, out NativeArray<TEntry> sortedEntries)
      {
        int passCount = rsParams.PassCount;
        sortedEntries = ((passCount & 1) == 0) ? rsParams.Entries : rsParams.TempEntries;

        var job = new RadixSortJob()
        {
          PassCount = passCount,
          Count = rsParams.EntryCount,
          Entries = rsParams.Entries,
          TempEntries = rsParams.TempEntries,
        };
        return job.Schedule(rsParams.Dependency);
      }
      [BurstCompile]
      public struct RadixSortJob : IJob
      {
        public int PassCount;
        public int Count;
        [NoAlias] public NativeArray<TEntry> Entries;
        [NoAlias] public NativeArray<TEntry> TempEntries;
        public void Execute()
        {
          Span<int> histogram = stackalloc int[RadixUtils.BinCount * PassCount];

          switch (PassCount)
          {
            case 1:
              for (int i = 0; i < Count; i++)
              {
                uint val = Entries[i].Key;
                histogram[0 * RadixUtils.BinCount + (int)(val & RadixUtils.Mask)]++;
              }
              break;
            case 2:
              for (int i = 0; i < Count; i++)
              {
                uint val = Entries[i].Key;
                histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 0)]++;
                histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 1)]++;
              }
              break;
            case 3:
              for (int i = 0; i < Count; i++)
              {
                uint val = Entries[i].Key;
                histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 0)]++;
                histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 1)]++;
                histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 2)]++;
              }
              break;
            case 4:
              for (int i = 0; i < Count; i++)
              {
                uint val = Entries[i].Key;
                histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 0)]++;
                histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 1)]++;
                histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 2)]++;
                histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 3)]++;
              }
              break;
          }

          NativeArray<TEntry> srcEntries = Entries;
          NativeArray<TEntry> dstEntries = TempEntries;

          for (int pass = 0; pass < PassCount; pass++)
          {
            int passOffset = pass * RadixUtils.BinCount;
            int currentBitsShift = pass * RadixUtils.BitsPerPass;
            int sum = 0;
            for (int i = 0; i < RadixUtils.BinCount; i++)
            {
              int count = histogram[passOffset + i];
              histogram[passOffset + i] = sum;
              sum += count;
            }

            for (int i = 0; i < Count; i++)
            {
              uint val = srcEntries[i].Key;
              int byteVal = (int)((val >> currentBitsShift) & RadixUtils.Mask);
              int targetIndex = histogram[passOffset + byteVal]++;
              dstEntries[targetIndex] = srcEntries[i];
            }
            (srcEntries, dstEntries) = (dstEntries, srcEntries);
          }
        }
      }
    }
  }

}
