using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;

namespace H2o.Sort
{
  public interface IEntry
  {
    uint Key { get; }
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct Entry : IEntry
  {
    public uint Key;
    public uint Payload;

    uint IEntry.Key => Key;
  }
  public struct RadixSortParams<TEntry>
    where TEntry : unmanaged, IEntry
  {
    public uint MaxKey;
    public int Count;
    public NativeArray<TEntry> Entries;
    public NativeArray<TEntry> TempEntries;
    public JobHandle Dependency;
    public readonly int PassCount => RadixUtils.GetPassCount(MaxKey);
  }
  public interface IRadixSort<TEntry>
    where TEntry : unmanaged, IEntry
  {
    public JobHandle Schedule(RadixSortParams<TEntry> rsParams, out NativeArray<TEntry> sortedEntries);
  }
  public sealed partial class RadixSort<TEntry> : IRadixSort<TEntry>, System.IDisposable
    where TEntry : unmanaged, IEntry
  {
    private bool _disposed = false;
    private int _parallelThreshold;
    private Serial _serial;
    private Parallel _parallel;
    public RadixSort(int parallelThreshold = 16384)
    {
      _parallelThreshold = parallelThreshold;
      _serial = new Serial();
      _parallel = new Parallel();
    }
    ~RadixSort()
    {
      Dispose(false);
    }
    public void Dispose()
    {
      Dispose(true);
      System.GC.SuppressFinalize(this);
    }
    void Dispose(bool disposing)
    {
      if (_disposed) return;

      if (disposing)
      {
        // Dispose managed state (managed objects).
        _parallel.Dispose();
      }

      // Free unmanaged resources.
      _disposed = true;
    }
    public JobHandle Schedule(RadixSortParams<TEntry> rsParams, out NativeArray<TEntry> sortedEntries)
    {
      if (rsParams.Count < _parallelThreshold)
      {
        return _serial.Schedule(rsParams, out sortedEntries);
      }
      return _parallel.Schedule(rsParams, out sortedEntries);
    }
    public JobHandle ScheduleSerial(RadixSortParams<TEntry> rsParams, out NativeArray<TEntry> sortedEntries)
    {
      return _serial.Schedule(rsParams, out sortedEntries);
    }
    public JobHandle ScheduleParallel(RadixSortParams<TEntry> rsParams, out NativeArray<TEntry> sortedEntries)
    {
      return _parallel.Schedule(rsParams, out sortedEntries);
    }
  }
}
