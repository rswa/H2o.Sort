using UnityEngine.Assertions;

namespace H2o.Sort
{
  /// <summary>
  /// Configuration parameters for the GPU Radix Sort operation.
  /// </summary>
  public struct RadixGpuSorterParams
  {
    /// <summary>
    /// Total number of grid cells (value range: 0 to CellCount - 1). 
    /// Used to determine the number of sorting passes required.
    /// </summary>
    public uint MaxKey;
    public uint KeyCount;
    public EntryGraphicsBuffers Entries;
    public EntryGraphicsBuffers TempEntries;
    public bool Validate() => Entries.Count >= KeyCount && TempEntries.Count >= KeyCount;

    public void AssertValid()
    {
      Assert.IsTrue(Validate(), $"{nameof(RadixGpuSorterParams)}: {nameof(KeyCount)} ({KeyCount}) " +
        $"exceeds capacity of {nameof(Entries)} or {nameof(TempEntries)}.");
    }

  }

}