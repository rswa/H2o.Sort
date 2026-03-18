using Unity.Collections;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace H2o.Sort.Sandbox
{
  public class RadixSortTest : MonoBehaviour
  {
    public int Count = 1024;
    public uint MaxKey = 65535;
    public uint RandomSeed = 12345;
    public int Iterations = 5;
    public bool EnableLogArray = true;
    public uint MaxLogArrayElements = 1024;

    protected EntryNativeArrays _rawEntries;
    protected EntryNativeArrays _entries;
    protected EntryNativeArrays _tempEntries;


    NativeArray<Entry> _rawEntriesV2;
    NativeArray<Entry> _entriesV2;
    NativeArray<Entry> _tempEntriesV2;
    protected Random _random;
    RadixSort<Entry> _sort;
    RadixSortParallel<Entry> _sortParallel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
      _random = new Random(RandomSeed);
      _rawEntries = new EntryNativeArrays(Count, Allocator.Persistent);
      _entries = new EntryNativeArrays(Count, Allocator.Persistent);
      _tempEntries = new EntryNativeArrays(Count, Allocator.Persistent);
      _rawEntriesV2 = new NativeArray<Entry>(Count, Allocator.Persistent);
      _entriesV2 = new NativeArray<Entry>(Count, Allocator.Persistent);
      _tempEntriesV2 = new NativeArray<Entry>(Count, Allocator.Persistent);
      _sort = new RadixSort<Entry>();
      _sortParallel = new RadixSortParallel<Entry>();

      NativeArray<uint> keys = _rawEntries.Keys;
      NativeArray<uint> payloads = _rawEntries.Payloads;
      uint EndKey = MaxKey + 1;
      for (int i = 0; i < Count; i++)
      {
        uint key = _random.NextUInt(EndKey);
        _rawEntries.Set(i, key, (uint)i);
        _rawEntriesV2[i] = new Entry()
        {
          Key = key,
          Payload = (uint)i
        };
      }

      SortTest();
      SortParallelTest();
    }

    protected virtual void OnDestroy()
    {
      if (_rawEntries.IsCreated) _rawEntries.Dispose();
      if (_entries.IsCreated) _entries.Dispose();
      if (_tempEntries.IsCreated) _tempEntries.Dispose();
      if (_rawEntriesV2.IsCreated) _rawEntriesV2.Dispose();
      if (_entriesV2.IsCreated) _entriesV2.Dispose();
      if (_tempEntriesV2.IsCreated) _tempEntriesV2.Dispose();
      _sortParallel?.Dispose();
    }

    void SortTest()
    {
      RadixSortParams<Entry> rsParams = new RadixSortParams<Entry>()
      {
        MaxKey = MaxKey,
        Count = Count,
        Entries = _entriesV2,
        TempEntries = _tempEntriesV2,
      };
      Stopwatch stopwatch = Stopwatch.StartNew();
      for (int i = 0; i < Iterations; i++)
      {
        _entriesV2.CopyFrom(_rawEntriesV2);
        stopwatch.Restart();
        _sort.Schedule(rsParams, out NativeArray<Entry> sortedEntries).Complete();
        TestUtils.LogElapsedTime($"SortV2({i})", stopwatch.Elapsed);
        if (i == 0)
        {
          TestUtils.ValidateSortedEntries(sortedEntries, _rawEntriesV2);

          if (EnableLogArray)
          {
            TestUtils.LogKeys("_rawEntries.Keys", _rawEntriesV2, MaxLogArrayElements);
            TestUtils.LogKeys("sortedEntries.Keys", sortedEntries, MaxLogArrayElements);
            TestUtils.LogPaylods("sortedEntries.Payloads", sortedEntries, MaxLogArrayElements);
          }
        }
      }
    }
    void SortParallelTest()
    {
      var rsParams = new RadixSortParams<Entry>()
      {
        MaxKey = MaxKey,
        Count = Count,
        Entries = _entriesV2,
        TempEntries = _tempEntriesV2,
      };
      System.TimeSpan totalTime = new System.TimeSpan(0);
      Stopwatch stopwatch = Stopwatch.StartNew();
      for (int i = 0; i < Iterations; i++)
      {
        _entriesV2.CopyFrom(_rawEntriesV2);
        stopwatch.Restart();
        _sortParallel.Schedule(rsParams, out NativeArray<Entry> sortedEntries).Complete();
        stopwatch.Stop();
        TestUtils.LogElapsedTime($"SortParallel({i})", stopwatch.Elapsed);
        if (i == 0)
        {
          TestUtils.ValidateSortedEntries(sortedEntries, _rawEntriesV2);

          if (EnableLogArray)
          {
            TestUtils.LogKeys("_rawEntries.Keys", _rawEntriesV2, MaxLogArrayElements);
            TestUtils.LogKeys("sortedEntries.Keys", sortedEntries, MaxLogArrayElements);
            TestUtils.LogPaylods("sortedEntries.Payloads", sortedEntries, MaxLogArrayElements);
          }
        }
        else
        {
          totalTime += stopwatch.Elapsed;
        }
      }
      if (Iterations > 1)
      {
        TestUtils.LogElapsedTime($"SortParallel average = ", totalTime / (Iterations - 1));
      }
    }
  }
}

