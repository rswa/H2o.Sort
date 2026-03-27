using UnityEngine;
using UnityEngine.Assertions;

namespace H2o.Sort
{

  [CreateAssetMenu(fileName = "RadixSortGpuSettings", menuName = "Scriptable Objects/H2o/RadixSortGpuSettings")]
  public class RadixSortGpuSettings : ScriptableObject
  {
    [SerializeField] ComputeShader _radixCount;
    [SerializeField] ComputeShader _radixScan;
    [SerializeField] ComputeShader _radixReorder;

    public ComputeShader RadixCount => _radixCount;
    public ComputeShader RadixScan => _radixScan;
    public ComputeShader RadixReorder => _radixReorder;

    [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
    public void AssertValid()
    {
      Assert.IsNotNull(_radixCount, $"[{nameof(RadixSortGpuSettings)}] {nameof(_radixCount)} is null.");
      Assert.IsNotNull(_radixScan, $"[{nameof(RadixSortGpuSettings)}] {nameof(_radixScan)} is null.");
      Assert.IsNotNull(_radixReorder, $"[{nameof(RadixSortGpuSettings)}] {nameof(_radixReorder)} is null.");
    }
  }
}
