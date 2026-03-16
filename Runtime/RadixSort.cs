using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace H2o.Sort
{
  public class RadixSort
  {
    [BurstCompile]
    public struct RadixSortJob : IJob
    {
      public int passCount;
      public int keyCount;
      [NoAlias] public NativeArray<uint> keys;
      [NoAlias] public NativeArray<uint> tempKeys;
      [NoAlias] public NativeArray<uint> payloads;
      [NoAlias] public NativeArray<uint> tempPayloads;
      public void Execute()
      {
        Span<int> histogram = stackalloc int[RadixUtils.BinCount * passCount];

        switch (passCount)
        {
          case 1:
            for (int i = 0; i < keyCount; i++)
            {
              uint val = keys[i];
              histogram[0 * RadixUtils.BinCount + (int)(val & RadixUtils.Mask)]++;
            }
            break;
          case 2:
            for (int i = 0; i < keyCount; i++)
            {
              uint val = keys[i];
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 0)]++;
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 1)]++;
            }
            break;
          case 3:
            for (int i = 0; i < keyCount; i++)
            {
              uint val = keys[i];
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 0)]++;
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 1)]++;
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 2)]++;
            }
            break;
          case 4:
            for (int i = 0; i < keyCount; i++)
            {
              uint val = keys[i];
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 0)]++;
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 1)]++;
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 2)]++;
              histogram[(int)RadixUtils.KeyToGlobalBinIndex(val, 3)]++;
            }
            break;
        }

        NativeArray<uint> srcKeys = keys;
        NativeArray<uint> srcPayloads = payloads;
        NativeArray<uint> dstKeys = tempKeys;
        NativeArray<uint> dstPayloads = tempPayloads;

        for (int pass = 0; pass < passCount; pass++)
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

          for (int i = 0; i < keyCount; i++)
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
