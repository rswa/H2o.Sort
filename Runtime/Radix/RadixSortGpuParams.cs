using UnityEngine;
using UnityEngine.Assertions;

namespace H2o.Sort
{
  /// <summary>
  /// Parameters for GPU radix sort.
  /// Entry layout depends on <see cref="EnablePayload"/>:
  /// <list type="bullet">
  /// <item>Enabled: [4 bytes key | 4 bytes payload]</item>
  /// <item>Disabled: [4 bytes key]</item>
  /// </list>
  /// </summary>
  public struct RadixSortGpuParams
  {
    public uint MaxKey;
    public uint EntryCount;
    public GraphicsBuffer Entries;
    public GraphicsBuffer TempEntries;
    public bool EnablePayload;
    public bool Validate() => Entries.count >= EntryCount && TempEntries.count >= EntryCount;

    public void AssertValid()
    {
      Assert.IsTrue(Validate(), $"{nameof(RadixSortGpuParams)}: {nameof(EntryCount)} ({EntryCount}) " +
        $"exceeds capacity of {nameof(Entries)} or {nameof(TempEntries)}.");
    }
  }
}