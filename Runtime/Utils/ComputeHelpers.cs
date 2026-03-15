using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace H2o.Sort
{
  public static class ComputeHelpers
  {
    public static void ReleaseBuffer(ref GraphicsBuffer buffer)
    {
      buffer?.Release();
      buffer = null;
    }
    public static void ReleaseBuffer<T>(ref ConstantBuffer<T> buffer) where T : struct
    {
      buffer?.Release();
      buffer = null;
    }
    public static void SetComputeUintParam(CommandBuffer cmd, ComputeShader shader, int propertyId, uint elementCount)
    {
      cmd.SetComputeIntParam(shader, propertyId, (int)elementCount);
    }
    public static void SetComputeUIntParam(CommandBuffer cmd, ComputeShader shader, string propertyName, uint elementCount)
    {
      cmd.SetComputeIntParam(shader, propertyName, (int)elementCount);
    }
    public static void DispatchCompute(CommandBuffer commandBuffer, ComputeShader shader, int kernelId, int elementCount)
    {
      Dispatch(commandBuffer, shader, kernelId, (uint)elementCount);
    }
    public static void DispatchCompute(CommandBuffer commandBuffer, ComputeShader shader, int kernelId, int2 elementCount)
    {
      DispatchCompute(commandBuffer, shader, kernelId, (uint2)elementCount);
    }
    public static void DispatchCompute(CommandBuffer commandBuffer, ComputeShader shader, int kernelId, int3 elementCount)
    {
      DispatchCompute(commandBuffer, shader, kernelId, (uint3)elementCount);
    }

    public static void Dispatch(CommandBuffer commandBuffer, ComputeShader shader, int kernelId, uint elementCount)
    {
      shader.GetKernelThreadGroupSizes(kernelId, out uint xGroupSize, out uint _, out uint _);
      uint groupCount = CeilDiv(elementCount, xGroupSize);
      commandBuffer.DispatchCompute(shader, kernelId, (int)groupCount, 1, 1);

    }
    public static void DispatchCompute(CommandBuffer commandBuffer, ComputeShader shader, int kernelId, uint2 elementCount)
    {
      shader.GetKernelThreadGroupSizes(kernelId, out uint xGroupSize, out uint yGroupSize, out uint _);

      uint2 groupSize = new uint2(xGroupSize, yGroupSize);
      uint2 groupCount = CeilDiv(elementCount, groupSize);
      commandBuffer.DispatchCompute(shader, kernelId, (int)groupCount.x, (int)groupCount.y, 1);
    }
    public static void DispatchCompute(CommandBuffer commandBuffer, ComputeShader shader, int kernelId, uint3 elementCount)
    {
      shader.GetKernelThreadGroupSizes(kernelId, out uint xGroupSize, out uint yGroupSize, out uint zGroupSize);

      uint3 groupSize = new uint3(xGroupSize, yGroupSize, zGroupSize);
      uint3 groupCount = CeilDiv(elementCount, groupSize);
      commandBuffer.DispatchCompute(shader, kernelId, (int)groupCount.x, (int)groupCount.y, (int)groupCount.z);
    }
    public static int CeilDiv(int n, int size)
    {
      return (n + size - 1) / size;
    }
    public static int2 CeilDiv(int2 n, int2 size)
    {
      return (n + size - 1) / size;
    }
    public static int3 CeilDiv(int3 n, int3 size)
    {
      return (n + size - 1) / size;
    }
    public static uint CeilDiv(uint n, uint size)
    {
      return (n + size - 1) / size;
    }
    public static uint2 CeilDiv(uint2 n, uint2 size)
    {
      return (n + size - 1) / size;
    }
    public static uint3 CeilDiv(uint3 n, uint3 size)
    {
      return (n + size - 1) / size;
    }
    public static void Dispatch(ComputeShader shader, int kernelId, uint elementCount)
    {
      shader.GetKernelThreadGroupSizes(kernelId, out uint xGroupSize, out uint _, out uint _);

      uint groupCount = CeilDiv(elementCount, xGroupSize);
      shader.Dispatch(kernelId, (int)groupCount, 1, 1);

    }
    public static void Dispatch(ComputeShader shader, int kernelId, uint2 elementCount)
    {
      shader.GetKernelThreadGroupSizes(kernelId, out uint xGroupSize, out uint yGroupSize, out uint _);

      uint2 groupSize = new uint2(xGroupSize, yGroupSize);
      uint2 groupCount = CeilDiv(elementCount, groupSize);
      shader.Dispatch(kernelId, (int)groupCount.x, (int)groupCount.y, 1);
    }
    public static void Dispatch(ComputeShader shader, int kernelId, uint3 elementCount)
    {
      shader.GetKernelThreadGroupSizes(kernelId, out uint xGroupSize, out uint yGroupSize, out uint zGroupSize);

      uint3 groupSize = new uint3(xGroupSize, yGroupSize, zGroupSize);
      uint3 groupCount = CeilDiv(elementCount, groupSize);
      shader.Dispatch(kernelId, (int)groupCount.x, (int)groupCount.y, (int)groupCount.z);
    }

    public static void Dispatch(ComputeShader shader, int kernelId, int elementCount)
    {
      Dispatch(shader, kernelId, (uint)elementCount);
    }
    public static void Dispatch(ComputeShader shader, int kernelId, int2 elementCount)
    {
      Dispatch(shader, kernelId, (uint2)elementCount);
    }
    public static void Dispatch(ComputeShader shader, int kernelId, int3 elementCount)
    {
      Dispatch(shader, kernelId, (uint3)elementCount);
    }
  }
}
