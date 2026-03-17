
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
    public static void ValidateSortedEntries(NativeArray<uint> keys, NativeArray<uint> payloads, NativeArray<uint> rawKeys)
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
        uint paylodToKey = rawKeys[(int)payload];
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
  }
}
