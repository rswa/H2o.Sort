using UnityEngine;
using UnityEngine.Assertions;

namespace H2o.Sort
{

  [CreateAssetMenu(fileName = "RadixGpuSorterSettings", menuName = "Scriptable Objects/H2o/RadixGpuSorterSettings")]
  public class RadixGpuSorterSettings : ScriptableObject
  {
    [SerializeField] ComputeShader _radixCount;
    [SerializeField] ComputeShader _radixScanDx12;
    [SerializeField] ComputeShader _radixReorder;

    public ComputeShader RadixCount => _radixCount;
    public ComputeShader RadixScanDx12 => _radixScanDx12;
    public ComputeShader RadixReorder => _radixReorder;

    [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
    public void AssertVald()
    {
      Assert.IsNotNull(_radixCount, $"{nameof(_radixCount)} is null");
      Assert.IsNotNull(_radixScanDx12, $"{nameof(_radixScanDx12)} is null");
      Assert.IsNotNull(_radixReorder, $"{nameof(_radixReorder)} is null");
    }
  }
}
