using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace H2o.Sort
{
  public struct NativeEntries
  {
    public NativeArray<uint> Keys;
    public NativeArray<uint> Payloads;
  }
  public struct RadixSortParams
  {
    public uint MaxKey;
    public int Count;
    public NativeEntries Entries;
    public NativeEntries TempEntries;
    public JobHandle Dependency;
    public readonly int PassCount => RadixUtils.GetPassCount(MaxKey);
  }
  public static class RadixSort
  {
    public static JobHandle Schedule(RadixSortParams rsParams, out NativeEntries sortedData)
    {
      int passCount = rsParams.PassCount;
      sortedData = ((passCount & 1) == 0) ? rsParams.Entries : rsParams.TempEntries;

      var job = new RadixSortJob()
      {
        PassCount = passCount,
        Count = rsParams.Count,
        Keys = rsParams.Entries.Keys,
        Payloads = rsParams.Entries.Payloads,
        TempKeys = rsParams.TempEntries.Keys,
        TempPayloads = rsParams.TempEntries.Payloads,
      };
      return job.Schedule(rsParams.Dependency);
    }

    [BurstCompile]
    public struct RadixSortJob : IJob
    {
      public int PassCount;
      public int Count;
      [NoAlias] public NativeArray<uint> Keys;
      [NoAlias] public NativeArray<uint> TempKeys;
      [NoAlias] public NativeArray<uint> Payloads;
      [NoAlias] public NativeArray<uint> TempPayloads;
      public void Execute()
      {
        Span<int> histogram = stackalloc int[RadixUtils.BinCount * PassCount];

        switch (PassCount)
        {
          case 1:
            for (int i = 0; i < Count; i++)
            {
              uint val = Keys[i];
              histogram[0 * RadixUtils.BinCount + (int)(val & RadixUtils.Mask)]++;
            }
            break;
          case 2:
            for (int i = 0; i < Count; i++)
            {
              uint val = Keys[i];
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 0)]++;
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 1)]++;
            }
            break;
          case 3:
            for (int i = 0; i < Count; i++)
            {
              uint val = Keys[i];
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 0)]++;
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 1)]++;
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 2)]++;
            }
            break;
          case 4:
            for (int i = 0; i < Count; i++)
            {
              uint val = Keys[i];
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 0)]++;
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 1)]++;
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 2)]++;
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 3)]++;
            }
            break;
        }

        NativeArray<uint> srcKeys = Keys;
        NativeArray<uint> srcPayloads = Payloads;
        NativeArray<uint> dstKeys = TempKeys;
        NativeArray<uint> dstPayloads = TempPayloads;

        for (int pass = 0; pass < PassCount; pass++)
        {
          int offset = pass * RadixUtils.BinCount;
          int currentBitsShift = pass * RadixUtils.BitsPerPass;
          int sum = 0;
          for (int i = 0; i < RadixUtils.BinCount; i++)
          {
            int count = histogram[offset + i];
            histogram[offset + i] = sum;
            sum += count;
          }

          for (int i = 0; i < Count; i++)
          {
            uint val = srcKeys[i];
            int byteVal = (int)((val >> currentBitsShift) & RadixUtils.Mask);
            int targetIndex = histogram[offset + byteVal]++;
            dstKeys[targetIndex] = srcKeys[i];
            dstPayloads[targetIndex] = srcPayloads[i];
          }
          (srcKeys, dstKeys) = (dstKeys, srcKeys);
          (srcPayloads, dstPayloads) = (dstPayloads, srcPayloads);
        }
      }
    }
  }
}
