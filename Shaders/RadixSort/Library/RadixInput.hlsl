#ifndef RADIX_INPUT_HLSL
#define RADIX_INPUT_HLSL

struct RadixParams
{
    uint passIndex; // Current iteration of the radix sort
    uint keyCount; // Total number of elements to be sorted
    uint blockCount; // Number of thread groups dispatched
    uint _padding; // Reserved to satisfy 16-byte alignment (std140/CB alignment)
};
ConstantBuffer<RadixParams> _RadixParams;



#endif // RADIX_INPUT_HLSL