using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace H2o.Sort
{
  public static class NativeCollectionExtentions
  {
    /// <summary>
    /// Clears the contents of a <see cref="NativeArray{T}"/> by setting all bytes to zero.
    /// </summary>
    /// <typeparam name="T">Unmanaged struct type.</typeparam>
    /// <param name="array">The target NativeArray to clear.</param>
    /// <remarks>
    /// Uses <see cref="UnsafeUtility.MemClear"/> for fast memory-level zeroing.
    /// Only safe for unmanaged structs.
    /// </remarks>
    public unsafe static void ClearMemory<T>(this NativeArray<T> array) where T : unmanaged
    {
      if (array.Length == 0) return;

      UnsafeUtility.MemClear(
          NativeArrayUnsafeUtility.GetUnsafePtr(array),
          (long)array.Length * UnsafeUtility.SizeOf<T>()
      );
    }
  }
}
