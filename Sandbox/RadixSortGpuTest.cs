using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Random = Unity.Mathematics.Random;

namespace H2o.Sort.Sandbox
{
  public class RadixSortGpuTest : MonoBehaviour
  {
    [Min(1)]
    public int KeyCount = 1024;
    [Min(1)]
    public uint MaxKey = 255;
    public uint RandomSeed = 12345;
    public bool EnableLogArray = true;
    public uint MaxLogArrayElements = 1024;
    [SerializeField] RadixSortGpuSettings _sortSettings;

    RadixSortGpu _sort;
    CommandBuffer _commandBuffer;


    EntryGraphicsBuffers _entries;
    EntryGraphicsBuffers _tempEntries;
    EntryNativeArrays _nativeEntries;
    EntryNativeArrays _readBackEntries;

    Random _random;
    uint _requestDoneCount = 0;
    StringBuilder _stringBuilder = new StringBuilder(4096 * 2);
    void Start()
    {
      _commandBuffer = new CommandBuffer
      {
        name = nameof(RadixSortGpuTest)
      };

      _sort = new RadixSortGpu((uint)KeyCount, _sortSettings);
      _entries = new EntryGraphicsBuffers(KeyCount);
      _tempEntries = new EntryGraphicsBuffers(KeyCount); // only for compute
      _nativeEntries = new EntryNativeArrays(KeyCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
      _readBackEntries = new EntryNativeArrays(KeyCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

      _random = new Random(RandomSeed);
      InitializeNativeEntries();

      TestSorting(true);
    }
    private void OnDestroy()
    {
      _commandBuffer?.Dispose();
      _sort?.Dispose();
      _entries.Dispose();
      _tempEntries.Dispose();
      _nativeEntries.Dispose();
      _readBackEntries.Dispose();
    }
    void Update()
    {
      TestSorting(false);
    }
    void ReadBackSortedKeys(AsyncGPUReadbackRequest request)
    {
      if (request.hasError)
      {
        Debug.Log($"{nameof(ReadBackSortedKeys)} request error");
      }
      else
      {
        LogArray(nameof(ReadBackSortedKeys), _readBackEntries.Keys);
        ProcessValidate();
      }

    }
    void ReadBackSortedPayloads(AsyncGPUReadbackRequest request)
    {
      if (request.hasError)
      {
        Debug.Log($"{nameof(ReadBackSortedPayloads)} request error");
      }
      else
      {
        LogArray(nameof(ReadBackSortedPayloads), _readBackEntries.Payloads);
        ProcessValidate();
      }
    }
    void ProcessValidate()
    {
      ++_requestDoneCount;
      if (_requestDoneCount == 2)
      {
        ValidateSortedEntries(_readBackEntries.Keys, _readBackEntries.Payloads);
      }
    }


    void LogArray(string name, NativeArray<uint> data)
    {
      if (EnableLogArray == false) return;

      uint size = math.min(MaxLogArrayElements, (uint)data.Length);

      _stringBuilder.Clear();
      for (int i = 0; i < size; i++)
      {
        _stringBuilder.Append($"{data[i],5}");
        if (i < data.Length - 1)
          _stringBuilder.Append(", ");
      }
      Debug.Log($"{name}: {_stringBuilder}");
    }
    void RquestGlobalHistogram(string name, GraphicsBuffer buffer)
    {
      _commandBuffer.RequestAsyncReadback(buffer, (request) =>
      {
        if (request.hasError)
        {
          Debug.Log($"ReadBack {name} Error");
        }
        else
        {
          NativeArray<uint> data = request.GetData<uint>();
          LogArray($"Readback {name}", data);
        }
      });
    }
    void TestSorting(bool enableValidation)
    {
      _nativeEntries.UploadTo(_entries, _nativeEntries.Count);

      _commandBuffer.Clear();
      RadixSortGpuParams sparams = new RadixSortGpuParams()
      {
        MaxKey = MaxKey,
        KeyCount = (uint)_entries.Count,
        Entries = _entries,
        TempEntries = _tempEntries,
      };
      EntryGraphicsBuffers sortedEntries = _sort.Dispatch(_commandBuffer, sparams);

      if (enableValidation && _requestDoneCount == 0)
      {
        RquestGlobalHistogram(nameof(_sort.GlobalHistogram), _sort.GlobalHistogram);
        RquestGlobalHistogram(nameof(_sort.BlockHistogramT), _sort.BlockHistogramT);
        RquestGlobalHistogram(nameof(_sort.BlockHistogram), _sort.BlockHistogram);
        NativeArray<uint> keys = _readBackEntries.Keys;
        NativeArray<uint> payloads = _readBackEntries.Payloads;
        _commandBuffer.RequestAsyncReadbackIntoNativeArray(ref keys, sortedEntries.Keys, ReadBackSortedKeys);
        _commandBuffer.RequestAsyncReadbackIntoNativeArray(ref payloads, sortedEntries.Payloads, ReadBackSortedPayloads);
      }
      Graphics.ExecuteCommandBuffer(_commandBuffer);
    }
    void ValidateSortedEntries(NativeArray<uint> keys, NativeArray<uint> payloads)
    {
      if (keys.Length == 0) return;

      Debug.Log($"ValidateSortedEntries Start");


      uint keyErrorCount = 0;
      int count = keys.Length;

      uint preKey = keys[0];
      for (int i = 1; i < count; i++)
      {
        uint currentCellId = keys[i];
        if (preKey > currentCellId)
        {
          ++keyErrorCount;
          //Debug.LogWarning($"{nameof(payloads)}[{i}] = {payloads[i]}, {nameof(keys)}[{i}] = {currentCellId} < {nameof(preKey)}({preKey})");
        }
        preKey = currentCellId;
      }
      uint payloadErrorCount = 0;
      for (int i = 0; i < count; i++)
      {
        uint key = keys[i];
        uint payload = payloads[i];
        uint paylodToKey = _nativeEntries.Keys[(int)payload];
        if (key != paylodToKey)
        {
          payloadErrorCount++;
          //Debug.LogWarning($"{payload} = {payload}, {key}({key}) != {nameof(paylodToKey)}({paylodToKey})");
        }
      }
      Debug.Log($"{nameof(keyErrorCount)}({keyErrorCount})");
      Debug.Log($"{nameof(payloadErrorCount)}({payloadErrorCount})");
      Debug.Log($"ValidateSortedEntries End");
    }
    void InitializeNativeEntries()
    {
      uint endKeyValue = MaxKey + 1;
      for (uint i = 0; i < KeyCount; ++i)
      {
        _nativeEntries.Set((int)i, _random.NextUInt(0, endKeyValue), i);
      }
      LogArray("InitializeNativeEntries.Keys", _nativeEntries.Keys);
      LogArray("InitializeNativeEntries.Payloads", _nativeEntries.Payloads);
    }
  }
}