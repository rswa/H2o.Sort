using System;
using Unity.Collections;
using static UnityEngine.GraphicsBuffer;

namespace H2o.Sort
{
  /// <summary>
  /// A collection of parallel NativeArrays representing the Entry SoA on the CPU.
  /// Mirrored with EntryGraphicsBuffers for efficient data transfer and Job System compatibility.
  /// </summary>
  public struct EntryNativeArrays : IDisposable
  {
    private NativeArray<uint> _keys;
    private NativeArray<uint> _payloads;

    public readonly NativeArray<uint> Keys => _keys;
    public readonly NativeArray<uint> Payloads => _payloads;

    public readonly bool IsCreated => _keys.IsCreated && _payloads.IsCreated;
    public readonly int Count => _keys.Length;

    public EntryNativeArrays(int count, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
    {
      _keys = new NativeArray<uint>(count, allocator, options);
      _payloads = new NativeArray<uint>(count, allocator, options);
    }

    public static void Upload(EntryNativeArrays src, int srcIndex, EntryGraphicsBuffers dst, int dstIndex, int count)
    {
      if ((dst.Keys.usageFlags & UsageFlags.LockBufferForWrite) != 0)
      {
        NativeArray<uint> lockedCellIds = dst.Keys.LockBufferForWrite<uint>(dstIndex, count);

        NativeArray<uint>.Copy(src._keys, srcIndex, lockedCellIds, 0, count);
        dst.Keys.UnlockBufferAfterWrite<uint>(count);
      }
      else
      {
        dst.Keys.SetData(src._keys, srcIndex, dstIndex, count);
      }
      if ((dst.Payloads.usageFlags & UsageFlags.LockBufferForWrite) != 0)
      {
        NativeArray<uint> lockedSourceIds = dst.Payloads.LockBufferForWrite<uint>(dstIndex, count);
        NativeArray<uint>.Copy(src._payloads, srcIndex, lockedSourceIds, 0, count);
        dst.Payloads.UnlockBufferAfterWrite<uint>(count);
      }
      else
      {
        dst.Payloads.SetData(src._payloads, srcIndex, dstIndex, count);
      }
    }
    public void UploadTo(int startIndex, EntryGraphicsBuffers dst, int dstIndex, int count)
    {
      Upload(this, startIndex, dst, dstIndex, count);
    }
    public void UploadTo(EntryGraphicsBuffers dst, int count)
    {
      Upload(this, 0, dst, 0, count);
    }
    public void Set(int index, uint key, uint payload)
    {
      _keys[index] = key;
      _payloads[index] = payload;
    }
    public void Get(int index, out uint key, out uint payload)
    {
      key = _keys[index];
      payload = _payloads[index];
    }
    public void Dispose()
    {
      if (_keys.IsCreated) _keys.Dispose();
      if (_payloads.IsCreated) _payloads.Dispose();
    }
  }
}
