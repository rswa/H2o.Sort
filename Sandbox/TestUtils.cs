
using System;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace H2o.Sort.Sandbox
{
  public static class TestUtils
  {
    static StringBuilder _stringBuilder = new StringBuilder(4096 * 2);
    public static void LogElapsedTime(string lable, long ticks)
    {
      LogElapsedTime(lable, new TimeSpan(ticks));
    }
    public static void LogElapsedTime(string lable, TimeSpan elapsedTime)
    {
      if (elapsedTime.Ticks >= TimeSpan.TicksPerSecond)
      {
        Debug.Log($"{lable} elapsed {elapsedTime.TotalSeconds:F3} seconds");
      }
      else if (elapsedTime.Ticks >= TimeSpan.TicksPerMillisecond)
      {
        Debug.Log($"{lable} elapsed {elapsedTime.TotalMilliseconds:F3} ms");
      }
      else
      {
        Debug.Log($"{lable} elapsed {elapsedTime.Ticks} ticks");
      }
    }
    public static void ValidateSortedEntries(NativeArray<uint> sortedKeys, NativeArray<uint> sortedPayloads, NativeArray<uint> rawKeys)
    {
      if (sortedKeys.Length == 0) return;

      Debug.Log($"ValidateSortedEntries Start");


      uint keyErrorCount = 0;
      int count = sortedKeys.Length;

      uint preKey = sortedKeys[0];
      for (int i = 1; i < count; i++)
      {
        uint currentKey = sortedKeys[i];
        if (preKey > currentKey)
        {
          ++keyErrorCount;
        }
        preKey = currentKey;
      }
      uint payloadErrorCount = 0;

      for (int i = 0; i < count; i++)
      {
        uint key = sortedKeys[i];
        uint payload = sortedPayloads[i];
        uint paylodToKey = rawKeys[(int)payload];
        if (key != paylodToKey)
        {
          payloadErrorCount++;
        }
      }
      Debug.Log($"{nameof(keyErrorCount)}({keyErrorCount})");
      Debug.Log($"{nameof(payloadErrorCount)}({payloadErrorCount})");
      Debug.Log($"ValidateSortedEntries End");
    }

    public static void ValidateSortedEntries(NativeArray<Entry> sortedEntries, NativeArray<Entry> rawEntries)
    {
      if (sortedEntries.Length == 0) return;

      Debug.Log($"ValidateSortedEntries Start");


      uint keyErrorCount = 0;
      int count = sortedEntries.Length;

      uint preKey = sortedEntries[0].Key;
      for (int i = 1; i < count; i++)
      {
        uint currentKey = sortedEntries[i].Key;
        if (preKey > currentKey)
        {
          ++keyErrorCount;
        }
        preKey = currentKey;
      }
      uint payloadErrorCount = 0;

      for (int i = 0; i < count; i++)
      {
        uint key = sortedEntries[i].Key;
        uint payload = sortedEntries[i].Payload;
        uint paylodToKey = rawEntries[(int)payload].Key;
        if (key != paylodToKey)
        {
          payloadErrorCount++;
        }
      }
      Debug.Log($"{nameof(keyErrorCount)}({keyErrorCount})");
      Debug.Log($"{nameof(payloadErrorCount)}({payloadErrorCount})");
      Debug.Log($"ValidateSortedEntries End");
    }
    public static void LogArray(string name, NativeArray<uint> data, uint maxLogArrayElements)
    {
      uint size = math.min(maxLogArrayElements, (uint)data.Length);

      _stringBuilder.Clear();
      for (int i = 0; i < size; i++)
      {
        _stringBuilder.Append($"{data[i],5}");
        if (i < data.Length - 1)
          _stringBuilder.Append(", ");
      }
      Debug.Log($"{name}: {_stringBuilder}");
    }
    public static void LogKeys(string name, NativeArray<Entry> entries, uint maxLogArrayElements)
    {
      uint size = math.min(maxLogArrayElements, (uint)entries.Length);

      _stringBuilder.Clear();
      for (int i = 0; i < size; i++)
      {
        _stringBuilder.Append($"{entries[i].Key,5}");
        if (i < entries.Length - 1)
          _stringBuilder.Append(", ");
      }
      Debug.Log($"{name}: {_stringBuilder}");
    }
    public static void LogPaylods(string name, NativeArray<Entry> entries, uint maxLogArrayElements)
    {
      uint size = math.min(maxLogArrayElements, (uint)entries.Length);

      _stringBuilder.Clear();
      for (int i = 0; i < size; i++)
      {
        _stringBuilder.Append($"{entries[i].Payload,5}");
        if (i < entries.Length - 1)
          _stringBuilder.Append(", ");
      }
      Debug.Log($"{name}: {_stringBuilder}");
    }
  }
}
