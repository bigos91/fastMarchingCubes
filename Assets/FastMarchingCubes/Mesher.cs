using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace MarchingCubes
{
	public partial class Mesher
	{
		// Precalculated array of indices (0,1,2,3,4,5...) because vertices are not shared, so we can just extract subarray
		// It may sound dumb, but it gives small boost (no list reallocations, no bounds checking, less cache pollution)
		// ~4mb but we probably saving that by not using indices list in each Mesher instance.
		// Also, 16bit indices can be used if needed.
		private const int PrecalculatedIndicesCount = 1000000;
		private static NativeArray<int> indicesPrecalc32bit;
		private static NativeArray<ushort> indicesPrecalc16bit;
		private static NativeArray<byte> triangulationTable;
		private static NativeArray<byte> cornerIndexA;
		private static NativeArray<byte> cornerIndexB;
		private static NativeArray<byte> cornerIndexMix;
		// with static native arrays we need that:
		private static int referenceCounter;



		public enum Mode { Naive, Simd32, Simd32Multithreaded }

		MeshingJob meshingJob;
		JobHandle meshingJobHandle;



		public Mesher()
		{
			System.Threading.Interlocked.Increment(ref referenceCounter);
			AllocateLookupArrays();
			Allocate();
		}
		private void Allocate()
		{
			meshingJob = new MeshingJob
			{
				triangulationTable = triangulationTable,
				cornerIndexA = cornerIndexA,
				cornerIndexB = cornerIndexB,
			};
			meshingJob.Allocate();
		}
		public void Dispose()
		{
			System.Threading.Interlocked.Decrement(ref referenceCounter);
			meshingJob.Dispose();
			DisposeStaticLookupArrays();
		}
		private static void DisposeStaticLookupArrays()
		{
			if (referenceCounter == 0)
			{
				indicesPrecalc32bit.Dispose();
				indicesPrecalc16bit.Dispose();
				triangulationTable.Dispose();
				cornerIndexMix.Dispose();
			}
		}



		public JobHandle StartMeshJob(Chunk chunk, Mode mode, int xStart = 0, int xStop = Chunk.ChunkSizeX - 1)
		{
			meshingJob.vertices.Clear();
			meshingJob.volume = chunk.data;
			meshingJob.mode = mode;

			if (mode == Mode.Simd32Multithreaded)
			{
				meshingJob.xStart = xStart;
				meshingJob.xStop = xStop;
				meshingJobHandle = meshingJob.Schedule();
				if (xStart >= (Chunk.ChunkSizeX - 1))
					Debug.LogWarning("Job started with xStart parameter outside bounds. It will do nothing.");
			}
			else
			{
				meshingJobHandle = meshingJob.Schedule();
			}

			return meshingJobHandle;
		}
		public bool IsFinished() => meshingJobHandle.IsCompleted;
		public void WaitForMeshJob() => meshingJobHandle.Complete();


		/// <summary>
		/// Combine meshes from different meshers.
		/// </summary>
		public void CombineMeshers(List<Mesher> meshers)
		{
			var verticesCount = 0;
			foreach(var mesher in meshers)
			{
				verticesCount += mesher.Vertices.Length;
			}

			meshingJob.vertices.Capacity = verticesCount + 9;

			foreach (var mesher in meshers)
			{
				if (mesher != this)
				{
					meshingJob.vertices.AddRange(mesher.meshingJob.vertices.AsArray());
				}
			}
		}



		public static VertexAttributeDescriptor VertexFormat => new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32);
		public static NativeArray<int> GetIndices(int count)
		{
			if (count > PrecalculatedIndicesCount)
				throw new System.Exception("Precalculated indices array is too short. Consider increasing PrecalculatedIndicesCount const in Mesher class.");
			return indicesPrecalc32bit.GetSubArray(0, count);
		}
		public static NativeArray<ushort> GetIndices16(int count)
		{
			if (count > ushort.MaxValue)
				throw new System.Exception("This should not happen. You are trying to get 16bit indices array for too many indices.");
			return indicesPrecalc16bit.GetSubArray(0, count);
		}
		public NativeArray<float3> Vertices => meshingJob.vertices.AsArray();
		public Bounds Bounds => meshingJob.bounds.item;
	}
}