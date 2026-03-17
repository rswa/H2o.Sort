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
    protected Random _random;

    RadixSortParallel _sortParallel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
      _random = new Random(RandomSeed);
      _rawEntries = new EntryNativeArrays(Count, Allocator.Persistent);
      _entries = new EntryNativeArrays(Count, Allocator.Persistent);
      _tempEntries = new EntryNativeArrays(Count, Allocator.Persistent);
      _sortParallel = new RadixSortParallel();


      NativeArray<uint> keys = _rawEntries.Keys;
      NativeArray<uint> payloads = _rawEntries.Payloads;
      uint EndKey = MaxKey + 1;
      for (int i = 0; i < Count; i++)
      {
        _rawEntries.Set(i, _random.NextUInt(EndKey), (uint)i);
      }

      SortTest();
      SortParallelTest();
    }

    protected virtual void OnDestroy()
    {
      if (_rawEntries.IsCreated) _rawEntries.Dispose();
      if (_entries.IsCreated) _entries.Dispose();
      if (_tempEntries.IsCreated) _tempEntries.Dispose();
      _sortParallel?.Dispose();
    }


    NativeEntries GetNativeEntries(EntryNativeArrays entries)
    {
      return new NativeEntries()
      {
        Keys = entries.Keys,
        Payloads = entries.Payloads,
      };
    }
    void SortTest()
    {
      RadixSortParams rsParams = new RadixSortParams()
      {
        MaxKey = MaxKey,
        Count = Count,
        Entries = GetNativeEntries(_entries),
        TempEntries = GetNativeEntries(_tempEntries),
      };
      Stopwatch stopwatch = Stopwatch.StartNew();
      for (int i = 0; i < Iterations; i++)
      {
        _entries.Keys.CopyFrom(_rawEntries.Keys);
        _entries.Payloads.CopyFrom(_rawEntries.Payloads);
        stopwatch.Restart();
        RadixSort.Schedule(rsParams, out NativeEntries sortedEntries).Complete();
        TestUtils.LogElapsedTime($"Sort({i})", stopwatch.Elapsed);
        if (i == 0)
        {
          TestUtils.ValidateSortedEntries(sortedEntries.Keys, sortedEntries.Payloads, _rawEntries.Keys);

          if (EnableLogArray)
          {
            TestUtils.LogArray("_rawEntries.Keys", sortedEntries.Keys, MaxLogArrayElements);
            TestUtils.LogArray("sortedEntries.Keys", sortedEntries.Keys, MaxLogArrayElements);
            TestUtils.LogArray("sortedEntries.Payloads", sortedEntries.Payloads, MaxLogArrayElements);
          }
        }
      }
    }
    void SortParallelTest()
    {
      RadixSortParams rsParams = new RadixSortParams()
      {
        MaxKey = MaxKey,
        Count = Count,
        Entries = GetNativeEntries(_entries),
        TempEntries = GetNativeEntries(_tempEntries),
      };
      Stopwatch stopwatch = Stopwatch.StartNew();
      for (int i = 0; i < Iterations; i++)
      {
        _entries.Keys.CopyFrom(_rawEntries.Keys);
        _entries.Payloads.CopyFrom(_rawEntries.Payloads);
        stopwatch.Restart();
        _sortParallel.Schedule(rsParams, out NativeEntries sortedEntries).Complete();
        TestUtils.LogElapsedTime($"SortParallel({i})", stopwatch.Elapsed);
        if (i == 0)
        {
          TestUtils.ValidateSortedEntries(sortedEntries.Keys, sortedEntries.Payloads, _rawEntries.Keys);

          if (EnableLogArray)
          {
            TestUtils.LogArray("_rawEntries.Keys", sortedEntries.Keys, MaxLogArrayElements);
            TestUtils.LogArray("sortedEntries.Keys", sortedEntries.Keys, MaxLogArrayElements);
            TestUtils.LogArray("sortedEntries.Payloads", sortedEntries.Payloads, MaxLogArrayElements);
          }
        }
      }
    }
  }
}

