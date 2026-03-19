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
    uint SubBlocks[RadixSubBlockCount];
};
struct RadixPayloadCache
{
    uint SubBlocks[RadixSubBlockCount];
};
struct RadixPrefixSumCache
{
    uint SubBlocks[RadixSubBlockCount];
};
struct RadixEntry
{
    uint Key;
#if defined(ENABLE_PAYLOAD)
    uint Payload;
#endif
};
#endif // RADIX_DEFINITIONS_HLSL