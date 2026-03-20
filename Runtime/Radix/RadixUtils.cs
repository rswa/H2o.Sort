using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace H2o.Sort
{
  public static class RadixUtils
  {
    // Constants in this file must match the definitions in RadixDefinitions.hlsl.
    public const int MaxPasses = 4;
    public const int BitsPerPass = 8;
    public const int BinCount = 1 << BitsPerPass;
    public const uint Mask = BinCount - 1;
    public const int GlobalBinCount = MaxPasses * BinCount;


    #region for RadixSortGpu only
    public const uint SubBlockCount = 8;
    public const uint BlockThreadGroupSize = BinCount;
    public const uint BlockSize = BlockThreadGroupSize * SubBlockCount;
    #endregion

    #region for RadixSort<TEntry> only
    public const int MinBlockSize = 1024;
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
    public static uint GetBlockHistogramSize(uint blockCount)
    {
      return blockCount * BinCount;
    }
    public static int GetPassCount(uint maxKey)
    {
      if (maxKey <= 0) return 0;
      int maxBit = 31 - math.lzcnt(maxKey);
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
    public static uint KeyToBinIndex(uint key, int pass)
    {
      return (key >> PassBitShifts[pass]) & Mask;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint KeyToGlobalBinIndex(uint key, int pass)
    {
      return GetPassGlobalBinIndex(KeyToBinIndex(key, pass), pass);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetGlobalBinCount(int passCount)
    {
      return PassGlobalBinOffsets[passCount];
    }
  }
}
