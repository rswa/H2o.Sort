#ifndef RADIX_DEFINITIONS_HLSL
#define RADIX_DEFINITIONS_HLSL


static const uint RadixMaxPasses = 4;
static const uint RadixBitsPerPass = 8;
static const uint RadixBinCount = 1 << RadixBitsPerPass;
static const uint RadixBitMask = RadixBinCount - 1;
static const uint RadixGlobalBinCount = RadixMaxPasses * RadixBinCount;
static const uint RadixSubBlockCount = 8;
static const uint RadixBlockThreadGroupSize = RadixBinCount;
static const uint RadixBlockSize = RadixBlockThreadGroupSize * RadixSubBlockCount;



struct RadixKeyCache
{
    uint subBlocks[RadixSubBlockCount];
};
struct RadixPayloadCache
{
    uint subBlocks[RadixSubBlockCount];
};
struct RadixPrefixSumCache
{
    uint subBlocks[RadixSubBlockCount];
};
#endif // RADIX_DEFINITIONS_HLSL