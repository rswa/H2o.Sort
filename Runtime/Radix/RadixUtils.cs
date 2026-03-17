using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace H2o.Sort
{
  public static class RadixUtils
  {
    public const int MaxPasses = 4;
    public const int BitsPerPass = 8;
    public const int BinCount = 1 << BitsPerPass;
    public const uint Mask = BinCount - 1;
    public const int MinBlockSize = 512;
    public const int JobBatchSize = 64; // for Job System innerLoopBatchCount
    public const uint GlobalBinCount = MaxPasses * BinCount;
    public const uint KeysPerBlock = BinCount;

    #region sub block version
    public const uint SubBlockCount = 8; // this is the same RadixSubBlockCount in RadixDefinitions.hlsl
    public const uint BlockThreadGroupSize = BinCount;
    public const uint BlockSize = BlockThreadGroupSize * SubBlockCount;
    #endregion
    static readonly int[] PassBitShifts =
    {
      0,
      BitsPerPass,
      BitsPerPass * 2,
      BitsPerPass * 3
    };

    static readonly uint[] PassGlobalBinOffsets =
    {
      0,
      BinCount,
      BinCount * 2,
      BinCount * 3,
      BinCount * 4
    };
    public static uint GetBlockCount(uint keyCount)
    {
      return CeilDiv(keyCount, KeysPerBlock);
    }
    public static uint GetBlockHistogramSize(uint blockCount)
    {
      return blockCount * BinCount;
    }
    public static uint GetKeyCount(uint blockCount)
    {
      return blockCount * KeysPerBlock;
    }
    public static int GetPassCount(uint maxValue)
    {
      if (maxValue <= 0) return 0;
      int maxBit = 31 - math.lzcnt(maxValue);
      return maxBit / BitsPerPass + 1;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CeilDiv(int total, int divisor)
    {
      return (total + divisor - 1) / divisor;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CeilDiv(uint total, uint divisor)
    {
      return (total + divisor - 1) / divisor;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetPassGlobalBinIndex(uint binIndex, int pass)
    {
      return PassGlobalBinOffsets[pass] + binIndex;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetPassGlobalBinOffset(uint pass)
    {
      return PassGlobalBinOffsets[pass];
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint KeyToBinIndex(uint cellId, int pass)
    {
      return (cellId >> PassBitShifts[pass]) & Mask;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint KeyToGlobalBinIndex(uint cellId, int pass)
    {
      return GetPassGlobalBinIndex(KeyToBinIndex(cellId, pass), pass);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetGlobalBinCount(int passCount)
    {
      return PassGlobalBinOffsets[passCount];
    }
  }
}
