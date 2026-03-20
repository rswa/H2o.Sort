# H2o.Sort

A high-performance sorting library for Unity, supporting both CPU and GPU execution.

## Requirements

- Unity 6000.3+
- Burst 1.8.28+
- Collections 1.2.4+
- Mathematics 1.2.6+
- Render Pipelines Core 17.3.0+

## Installation

Open `Packages/manifest.json` and add:

```json
{
  "dependencies": {
    "com.h2o.sort": "https://github.com/rswa/H2o.Sort.git"
  }
}
```

Or in Unity Package Manager → Add package from git URL:

```
https://github.com/rswa/H2o.Sort.git
```

## Usage

### RadixSort (CPU)

```csharp
using H2o.Sort;

var rsParams = new RadixSortParams<Entry>
{
    MaxKey = 65535,
    EntryCount = _entries.Length,
    Entries = entries,
    TempEntries = tempEntries,
};

// Single-threaded
IRadixSort<Entry> sorter = new RadixSort<Entry>.Serial();
sorter.Schedule(rsParams, out NativeArray<Entry> sortedEntries).Complete();

// Multi-threaded
IRadixSort<Entry> sorter = new RadixSort<Entry>.Parallel();
sorter.Schedule(rsParams, out NativeArray<Entry> sortedEntries).Complete();

// Adaptive (automatically selects Serial or Parallel based on entry count)
IRadixSort<Entry> sorter = new RadixSort<Entry>();
sorter.Schedule(rsParams, out NativeArray<Entry> sortedEntries).Complete();
```

### RadixSortGpu (GPU)

```csharp
using H2o.Sort;

var sparams = new RadixSortGpuParams
{
    MaxKey = 65535,
    EntryCount = (uint)entriesBuffer.count,
    Entries = entriesBuffer,
    TempEntries = tempBuffer,
    EnablePayload = true
};

GraphicsBuffer sortedEntries = sort.Dispatch(commandBuffer, sparams);

Graphics.ExecuteCommandBuffer(commandBuffer);
```

#### Entry Layout

| `EnablePayload` | Layout |
|---|---|
| `false` | `[4 bytes key]` |
| `true` | `[4 bytes key \| 4 bytes payload]` |

## Benchmarks

**2,097,152 elements, MaxKey = 65535, with payload**

| Method | Hardware | Time |
|---|---|---|
| RadixSortGpu | GTX 1060 | 1.1ms |
| RadixSort.Parallel | Intel E3-1230 v2 | 11.088ms |
| RadixSort.Serial | Intel E3-1230 v2 | 41.161ms |

## Sandbox

Test code is located in the `Sandbox` assembly (`com.h2o.sort.sandbox`) for development use only and is not included in production builds.

## Known Issues

- `RadixSortGpu`: WaveSize < 16 is not yet supported. This will be addressed in a future update.

## License

MIT