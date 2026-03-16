using System;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace H2o.Sort
{
  public struct EntryGraphicsBuffers : IDisposable
  {
    public const int Stride = sizeof(uint); // 4 bytes

    private GraphicsBuffer _keys;
    private GraphicsBuffer _payloads;

    /// <summary> Stride: sizeof(uint) </summary>
    public readonly GraphicsBuffer Keys => _keys;
    /// <summary> Stride: sizeof(uint) </summary>
    public readonly GraphicsBuffer Payloads => _payloads;
    public readonly bool IsCreated => _keys != null && _payloads != null;
    public readonly int Count => _keys?.count ?? 0;
    public EntryGraphicsBuffers(int count) : this(UsageFlags.None, count) { }
    public EntryGraphicsBuffers(UsageFlags usageFlags, int count)
    {
      _keys = new GraphicsBuffer(Target.Structured, usageFlags, count, Stride);
      _payloads = new GraphicsBuffer(Target.Structured, usageFlags, count, Stride);
    }
    public void Dispose()
    {
      _keys?.Dispose();
      _keys = null;
      _payloads?.Dispose();
      _payloads = null;
    }
  }
}
