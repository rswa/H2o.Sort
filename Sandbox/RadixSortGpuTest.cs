using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using Random = Unity.Mathematics.Random;

namespace H2o.Sort.Sandbox
{
  public class RadixSortGpuTest : MonoBehaviour
  {
    [Min(1)]
    public int EntryCount = 1024;
    [Min(1)]
    public uint MaxKey = 255;
    public uint RandomSeed = 12345;
    public bool EnableLogArray = true;
    public uint MaxLogArrayElements = 1024;
    [SerializeField] RadixSortGpuSettings _sortSettings;

    RadixSortGpu _sort;
    CommandBuffer _commandBuffer;


    NativeArray<Entry> _rawEntries;
    GraphicsBuffer _entries;
    GraphicsBuffer _tempEntries;


    NativeArray<uint> _rawKeyEntries;
    GraphicsBuffer _keyEntries;
    GraphicsBuffer _tempKeyEntries;

    Random _random;
    void Start()
    {
      _commandBuffer = new CommandBuffer
      {
        name = nameof(RadixSortGpuTest)
      };

      _sort = new RadixSortGpu((uint)EntryCount, _sortSettings);

      _rawEntries = new NativeArray<Entry>(EntryCount, Allocator.Persistent);
      _entries = new GraphicsBuffer(GraphicsBuffer.Target.Structured, EntryCount, UnsafeUtility.SizeOf<Entry>());
      _tempEntries = new GraphicsBuffer(GraphicsBuffer.Target.Structured, EntryCount, UnsafeUtility.SizeOf<Entry>());


      _rawKeyEntries = new NativeArray<uint>(EntryCount, Allocator.Persistent);
      _keyEntries = new GraphicsBuffer(GraphicsBuffer.Target.Structured, EntryCount, UnsafeUtility.SizeOf<uint>());
      _tempKeyEntries = new GraphicsBuffer(GraphicsBuffer.Target.Structured, EntryCount, UnsafeUtility.SizeOf<uint>());

      _random = new Random(RandomSeed);
      InitializeRawEntries();

      TestSorting(true);
    }
    private void OnDestroy()
    {
      _commandBuffer?.Dispose();
      _sort?.Dispose();
      if (_rawEntries.IsCreated) _rawEntries.Dispose();
      _entries?.Dispose();
      _tempEntries.Dispose();

      if (_rawKeyEntries.IsCreated) _rawKeyEntries.Dispose();
      _keyEntries?.Dispose();
      _tempKeyEntries.Dispose();
    }
    void Update()
    {
      TestSorting(false);
    }
    void TestSorting(bool enableValidation)
    {
      _commandBuffer.Clear();

      _entries.SetData(_rawEntries);
      RadixSortGpuParams sparams = new RadixSortGpuParams()
      {
        MaxKey = MaxKey,
        EntryCount = (uint)_entries.count,
        Entries = _entries,
        TempEntries = _tempEntries,
        EnablePayload = true
      };
      _commandBuffer.BeginSample("SortEntry");
      GraphicsBuffer sortedEntries = _sort.Dispatch(_commandBuffer, sparams);
      _commandBuffer.EndSample("SortEntry");
      if (enableValidation)
      {
        _commandBuffer.RequestAsyncReadback(sortedEntries, (request) =>
        {
          if (request.hasError)
          {
            return;
          }

          NativeArray<Entry> entries = request.GetData<Entry>(0);
          Debug.Log($"================= RadixSortGpu Sort Entry =================");
          TestUtils.ValidateSortedEntries(entries, _rawEntries);
        });
      }
      _keyEntries.SetData(_rawKeyEntries);
      sparams.Entries = _keyEntries;
      sparams.TempEntries = _tempKeyEntries;
      sparams.EnablePayload = false;

      _commandBuffer.BeginSample("SortKeyEntry");
      GraphicsBuffer sortedKeyEntries = _sort.Dispatch(_commandBuffer, sparams);
      _commandBuffer.EndSample("SortKeyEntry");
      if (enableValidation)
      {
        _commandBuffer.RequestAsyncReadback(sortedKeyEntries, (request) =>
        {
          if (request.hasError)
          {
            return;
          }

          NativeArray<uint> entries = request.GetData<uint>(0);
          Debug.Log($"================= RadixSortGpu Sort Key=================");
          TestUtils.ValidateSortedKeyEntries(entries, _rawKeyEntries);
        });
      }


      Graphics.ExecuteCommandBuffer(_commandBuffer);
    }

    void InitializeRawEntries()
    {
      uint endKeyValue = MaxKey + 1;
      for (uint i = 0; i < EntryCount; ++i)
      {
        uint key = _random.NextUInt(0, endKeyValue);
        uint payload = i;
        _rawEntries[(int)i] = new Entry()
        {
          Key = key,
          Payload = payload
        };
        _rawKeyEntries[(int)i] = key;
      }
      if (EnableLogArray)
      {
        TestUtils.LogKeys("InitializeRawEntries.Keys", _rawEntries, MaxLogArrayElements);
        TestUtils.LogPaylods("InitializeRawEntries.Payloads", _rawEntries, MaxLogArrayElements);
      }
    }
  }
}