#ifndef RADIX_UTILS_DX12_HLSL
#define RADIX_UTILS_DX12_HLSL


#include "RadixUtils.hlsl"

uint RadixWaveGetFirstLowLane(uint4 mask)
{
    const uint waveSize = WaveGetLaneCount();
    
    [branch]
    if (mask.x != 0)
        return firstbitlow(mask.x);
    [branch]
    if (waveSize > 32 && mask.y != 0)
        return firstbitlow(mask.y) + 32;
    [branch]
    if (waveSize > 64 && mask.z != 0)
        return firstbitlow(mask.z) + 64;
    [branch]
    if (waveSize > 96 && mask.w != 0)
        return firstbitlow(mask.w) + 96;

    return MaxUint;
}

/**
 * Calculates both the exclusive prefix sum and the total population count
 * of a 128-bit lane mask in a single pass using out parameters.
 * Optimized for static dead code elimination.
 */
void RadixWaveScanBits(uint4 waveBits, out uint prefixSum, out uint totalCount)
{
    const uint laneCount = WaveGetLaneCount();
    const uint lane = WaveGetLaneIndex();
    prefixSum = 0;
    totalCount = 0;

    // Segment 0: Lanes 0-31 (Always active)
    uint b0 = waveBits.x;
    totalCount += countbits(b0);
    prefixSum += countbits(b0 & ((lane < 32) ? (1u << lane) - 1 : 0xFFFFFFFF));

    // Segment 1: Lanes 32-63
    if (laneCount > 32)
    {
        uint b1 = waveBits.y;
        totalCount += countbits(b1);
        prefixSum += countbits(b1 & ((lane < 32) ? 0 : (lane < 64 ? (1u << (lane - 32)) - 1 : 0xFFFFFFFF)));
    }

    // Segment 2: Lanes 64-95
    if (laneCount > 64)
    {
        uint b2 = waveBits.z;
        totalCount += countbits(b2);
        prefixSum += countbits(b2 & ((lane < 64) ? 0 : (lane < 96 ? (1u << (lane - 64)) - 1 : 0xFFFFFFFF)));
    }

    // Segment 3: Lanes 96-127
    if (laneCount > 96)
    {
        uint b3 = waveBits.w;
        totalCount += countbits(b3);
        prefixSum += countbits(b3 & ((lane < 96) ? 0 : (1u << (lane - 96)) - 1));
    }
}
uint RadixWavePrefixSumBits(uint4 waveBits)
{
    const uint laneCount = WaveGetLaneCount();
    const uint lane = WaveGetLaneIndex();
    

    // Segment 0: Lanes 0-31 (Always active)
    uint b0 = waveBits.x;
    uint prefixSum = countbits(b0 & ((lane < 32) ? (1u << lane) - 1 : 0xFFFFFFFF));

    // Segment 1: Lanes 32-63
    if (laneCount > 32)
    {
        uint b1 = waveBits.y;
        prefixSum += countbits(b1 & ((lane < 32) ? 0 : (lane < 64 ? (1u << (lane - 32)) - 1 : 0xFFFFFFFF)));
    }

    // Segment 2: Lanes 64-95
    if (laneCount > 64)
    {
        uint b2 = waveBits.z;
        prefixSum += countbits(b2 & ((lane < 64) ? 0 : (lane < 96 ? (1u << (lane - 64)) - 1 : 0xFFFFFFFF)));
    }

    // Segment 3: Lanes 96-127
    if (laneCount > 96)
    {
        uint b3 = waveBits.w;
        prefixSum += countbits(b3 & ((lane < 96) ? 0 : (1u << (lane - 96)) - 1));
    }
    return prefixSum;
}

uint RadixWaveCountBits(uint4 waveBits)
{
    const uint laneCount = WaveGetLaneCount();
    
    // Segment 0: Lanes 0-31 (Always active)
    uint count = countbits(waveBits.x);
    // Segment 1: Lanes 32-63
    if (laneCount > 32)
    {
        count += countbits(waveBits.y);
    }
    // Segment 2: Lanes 64-95
    if (laneCount > 64)
    {
        count += countbits(waveBits.z);
    }
    // Segment 3: Lanes 96-127
    if (laneCount > 96)
    {
        count += countbits(waveBits.w);
    }
    return count;
}
/**
 * Identifies all lanes in the wave sharing the same radix binIndex.
 * Optimized for minimal instruction count and register pressure.
 */
uint4 RadixWaveBinBallot(uint binIndex)
{
    // Initialize with the current execution mask to exclude inactive lanes.
    // Once a lane bit is 0, the subsequent &= operations will keep it 0.
    uint4 waveBallot = WaveActiveBallot(true);
    [unroll]
    for (uint i = 0; i < RadixBitsPerPass; ++i)
    {
        const bool bit = (binIndex >> i) & 1;
        const uint4 b = WaveActiveBallot(bit);
        waveBallot &= bit ? b : ~b;
    }

    return waveBallot;
}

#endif // RADIX_UTILS_DX12_HLSL