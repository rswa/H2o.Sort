#ifndef RADIX_INPUT_HLSL
#define RADIX_INPUT_HLSL

struct RadixParams
{
    uint PassIndex; // Current iteration of the radix sort
    uint EntryCount; // Total number of elements to be sorted
    uint BlockCount; // Number of thread groups dispatched
    uint _padding; // Reserved to satisfy 16-byte alignment (std140/CB alignment)
};
ConstantBuffer<RadixParams> _RadixParams;



#endif // RADIX_INPUT_HLSL