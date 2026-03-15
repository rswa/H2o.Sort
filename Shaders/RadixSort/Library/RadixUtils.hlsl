#ifndef RADIX_UTILS_HLSL
#define RADIX_UTILS_HLSL

#include "RadixDefinitions.hlsl"
#include "RadixInput.hlsl"

static const uint MaxUint = 0xffffffff;

inline uint CeilDiv(uint n, uint size)
{
    return (n + size - 1) / size;
}

inline uint2 CeilDiv(uint2 n, uint2 size)
{
    return (n + size - 1) / size;
}

inline uint3 CeilDiv(uint3 n, uint3 size)
{
    return (n + size - 1) / size;
}
inline uint4 CeilDiv(uint4 n, uint4 size)
{
    return (n + size - 1) / size;
}
inline uint RadixGetPassGlobalBinOffset(uint passIndex)
{
    uint result;
    // Static resolution: compiler constant-folds this switch to eliminate runtime branching.
    switch (passIndex)
    {
        case 0: result = 0; break;
        case 1: result = RadixBinCount; break;
        case 2: result = RadixBinCount * 2; break;
        case 3: result = RadixBinCount * 3; break;
        case 4: result = RadixBinCount * 4; break;
        default: result = 0; break;
    }
    return result;
}
inline uint RadixGetGlobalBinCount(uint passCount)
{
    return RadixGetPassGlobalBinOffset(passCount);
}
inline uint RadixGetPassBitShift(int passIndex)
{
    uint result;
    
    // Static resolution: compiler constant-folds this switch to eliminate runtime branching.
    switch (passIndex)
    {
        case 0: result = 0; break;
        case 1: result = RadixBitsPerPass; break;
        case 2: result = RadixBitsPerPass * 2; break;
        case 3: result = RadixBitsPerPass * 3; break;
        default: result = 0; break;
    }
    return result;
}
inline uint RadixGetPassGlobalBinIndex(uint binIndex, uint passIndex)
{
    return RadixGetPassGlobalBinOffset(passIndex) + binIndex;
}
// Extracts the bin index (digit) for the current sorting pass.
// Optimized with dual-mechanism: Constant Folding + Uniform Static Branching.
// Resolves to: (key >> CONST_SHIFT) & CONST_MASK
inline uint RadixKeyToBinIndex(uint key, uint passIndex)
{
    return ((key >> RadixGetPassBitShift(passIndex)) & RadixBitMask);
}
inline uint RadixKeyToPassGlobalBinIndex(uint key, uint passIndex)
{
    return RadixGetPassGlobalBinIndex(RadixKeyToBinIndex(key, passIndex), passIndex);
}

inline uint RadixHistSubBinIndex(uint subIndex, uint binIndex)
{
    return subIndex * RadixBinCount + binIndex;
}

// for _BlockHistogramT
inline uint RadixHistBinBlockIndex(uint blockIndex, uint binIndex)
{
    return binIndex * _RadixParams.blockCount + blockIndex;
}
// for _BlockHistogram
inline uint RadixHistBlockBinIndex(uint blockIndex, uint binIndex)
{
    return RadixHistSubBinIndex(blockIndex, binIndex);
}
#endif // RADIX_UTILS_HLSL