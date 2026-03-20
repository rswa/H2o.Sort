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

    [Header("Log Settings")]
    public bool EnableLogArray = true;
    public uint MaxLogArrayElements = 1024;
    public bool EnableLogAllRun = false;


    NativeArray<Entry> _rawEntries;
    NativeArray<Entry> _entries;
    NativeArray<Entry> _tempEntries;
    protected Random _random;
    RadixSort<Entry>.Serial _sort;
    RadixSort<Entry>.Parallel _sortParallel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
      _random = new Random(RandomSeed);
      _rawEntries = new NativeArray<Entry>(Count, Allocator.Persistent);
      _entries = new NativeArray<Entry>(Count, Allocator.Persistent);
      _tempEntries = new NativeArray<Entry>(Count, Allocator.Persistent);
      _sort = new RadixSort<Entry>.Serial();
      _sortParallel = new RadixSort<Entry>.Parallel();

      uint EndKey = MaxKey + 1;
      for (int i = 0; i < Count; i++)
      {
        uint key = _random.NextUInt(EndKey);
        _rawEntries[i] = new Entry()
        {
          Key = key,
          Payload = (uint)i
        };
      }

      TestSorting(false);
      TestSorting(true);
    }

    protected virtual void OnDestroy()
    {
      if (_rawEntries.IsCreated) _rawEntries.Dispose();
      if (_entries.IsCreated) _entries.Dispose();
      if (_tempEntries.IsCreated) _tempEntries.Dispose();
      _sortParallel?.Dispose();
    }

    void TestSorting(bool parallel)
    {
      string parallelState = parallel ? "Parallel" : "Single";
      Debug.Log($"=================== RadixSort {parallelState} ===================");
      RadixSortParams<Entry> rsParams = new RadixSortParams<Entry>()
      {
        MaxKey = MaxKey,
        EntryCount = _entries.Length,
        Entries = _entries,
        TempEntries = _tempEntries,
      };
      IRadixSort<Entry> radixSort = parallel ? _sortParallel : _sort;
      System.TimeSpan minTime = new System.TimeSpan(long.MaxValue);
      System.TimeSpan maxTime = new System.TimeSpan(0);
      System.TimeSpan totalTime = new System.TimeSpan(0);
      Stopwatch stopwatch = Stopwatch.StartNew();
      for (int i = 0; i < Iterations; i++)
      {
        _entries.CopyFrom(_rawEntries);
        stopwatch.Restart();
        radixSort.Schedule(rsParams, out NativeArray<Entry> sortedEntries).Complete();
        stopwatch.Stop();
        if (EnableLogAllRun)
        {
          Debug.Log($"index = {i}, elapsed = {stopwatch.Elapsed.ToUnitString()}");
        }
        if (i == 0)
        {
          TestUtils.ValidateSortedEntries(sortedEntries, _rawEntries);

          if (EnableLogArray)
          {
            TestUtils.LogKeys("_rawEntries.Keys", _rawEntries, MaxLogArrayElements);
            TestUtils.LogKeys("sortedEntries.Keys", sortedEntries, MaxLogArrayElements);
            TestUtils.LogPaylods("sortedEntries.Payloads", sortedEntries, MaxLogArrayElements);
          }
        }
        else
        {
          totalTime += stopwatch.Elapsed;
          if (stopwatch.Elapsed < minTime) minTime = stopwatch.Elapsed;
          if (stopwatch.Elapsed > maxTime) maxTime = stopwatch.Elapsed;
        }
      }
      Debug.Log($"min = {minTime.ToUnitString()}, max = {maxTime.ToUnitString()}");
      if (Iterations > 1)
      {
        Debug.Log($"average = {(totalTime / (Iterations - 1)).ToUnitString()}");
      }
    }
  }
}

