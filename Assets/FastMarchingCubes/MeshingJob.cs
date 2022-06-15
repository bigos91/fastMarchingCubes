using UnityEngine;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace MarchingCubes
{
	[BurstCompile(
		CompileSynchronously = true,
		DisableSafetyChecks = true,
		FloatMode = FloatMode.Fast,
		FloatPrecision = FloatPrecision.Low)]
	public struct MeshingJob : IJob, System.IDisposable
	{
		public Mesher.Mode mode;
		public int xStart;
		public int xStop;

		[ReadOnly] public NativeArray<byte> triangulationTable;
		[ReadOnly] public NativeArray<byte> cornerIndexA;
		[ReadOnly] public NativeArray<byte> cornerIndexB;

		[ReadOnly] public NativeArray<sbyte> volume;
		[NativeDisableParallelForRestriction] public NativeList<float3> vertices;
		public UnsafePointer<Bounds> bounds;


		[BurstDiscard]
		public void Allocate()
		{
			vertices = new NativeList<float3>(500, Allocator.Persistent);
			bounds = new UnsafePointer<Bounds>(default);
		}

		[BurstDiscard]
		public void Dispose()
		{
			vertices.Dispose();
			bounds.Dispose();
		}


		public void Execute()
		{
			switch (mode)
			{
				case Mesher.Mode.Naive:
					DefaultImplementation();
					break;
				case Mesher.Mode.Simd32:
					SIMDChunkSizeZ32(0, Chunk.ChunkSizeX - 1);
					break;
				case Mesher.Mode.Simd32Multithreaded:
					SIMDChunkSizeZ32(xStart, xStop);
					break;
				default:
					break;
			}
		}


		private unsafe void DefaultImplementation()
		{
			float4* samples = stackalloc float4[8];

			for (int x = 0; x < Chunk.ChunkSizeX - 1; x++)
			{
				for (int y = 0; y < Chunk.ChunkSizeY - 1; y++)
				{
					for (int z = 0; z < Chunk.ChunkSizeZ - 1; z++)
					{
						const int flatOffsetX = Chunk.ChunkSizeY * Chunk.ChunkSizeZ;
						const int flatOffsetY = Chunk.ChunkSizeZ;
						const int flatOffsetZ = 1;
						var flatIndex = FlattenIndex(x, y, z);

						samples[0] = new float4(x + 0, y + 0, z + 0, volume[flatIndex]);                                            // volume[FlattenIndex(x + 0, y + 0, z + 0)]);
						samples[1] = new float4(x + 1, y + 0, z + 0, volume[flatIndex + flatOffsetX]);                              // volume[FlattenIndex(x + 1, y + 0, z + 0)]);
						samples[2] = new float4(x + 1, y + 0, z + 1, volume[flatIndex + flatOffsetX + flatOffsetZ]);                // volume[FlattenIndex(x + 1, y + 0, z + 1)]);
						samples[3] = new float4(x + 0, y + 0, z + 1, volume[flatIndex + flatOffsetZ]);                              // volume[FlattenIndex(x + 0, y + 0, z + 1)]);
						samples[4] = new float4(x + 0, y + 1, z + 0, volume[flatIndex + flatOffsetY]);                              // volume[FlattenIndex(x + 0, y + 1, z + 0)]);
						samples[5] = new float4(x + 1, y + 1, z + 0, volume[flatIndex + flatOffsetX + flatOffsetY]);                // volume[FlattenIndex(x + 1, y + 1, z + 0)]);
						samples[6] = new float4(x + 1, y + 1, z + 1, volume[flatIndex + flatOffsetX + flatOffsetY + flatOffsetZ]);  // volume[FlattenIndex(x + 1, y + 1, z + 1)]);
						samples[7] = new float4(x + 0, y + 1, z + 1, volume[flatIndex + flatOffsetY + flatOffsetZ]);                // volume[FlattenIndex(x + 0, y + 1, z + 1)]);

						int cornerMask = 0;

						if (samples[0].w < 0.0f) cornerMask |= 1 << 0;	// order of bits is different than in original marching cubes
						if (samples[1].w < 0.0f) cornerMask |= 1 << 1;	// only to make simd stuff easier
						if (samples[2].w < 0.0f) cornerMask |= 1 << 5;	// read Mesher.Arrays.cs for more info
						if (samples[3].w < 0.0f) cornerMask |= 1 << 4;	
						if (samples[4].w < 0.0f) cornerMask |= 1 << 2;	// default order: 76 54 32 10
						if (samples[5].w < 0.0f) cornerMask |= 1 << 3;	// new order	: 67 23 54 10
						if (samples[6].w < 0.0f) cornerMask |= 1 << 7;
						if (samples[7].w < 0.0f) cornerMask |= 1 << 6;

						if (cornerMask == 0 || cornerMask == 255)
							continue;

						cornerMask *= Mesher.TriangulationSubTableLenght;

						bounds.item.Encapsulate(new Vector3(x, y, z));
						bounds.item.Encapsulate(new Vector3(x + 1, y + 1, z + 1));

						for (; triangulationTable[cornerMask] != 99; cornerMask += 3)
						{
							vertices.Length = vertices.Length + 3; // make room for 3 vertices
							int a0 = cornerIndexA[triangulationTable[cornerMask]];
							int b0 = cornerIndexB[triangulationTable[cornerMask]];
							int a1 = cornerIndexA[triangulationTable[cornerMask + 1]];
							int b1 = cornerIndexB[triangulationTable[cornerMask + 1]];
							int a2 = cornerIndexA[triangulationTable[cornerMask + 2]];
							int b2 = cornerIndexB[triangulationTable[cornerMask + 2]];
							vertices[vertices.Length - 1] = InterpolateVerts(samples[a0], samples[b0]);
							vertices[vertices.Length - 2] = InterpolateVerts(samples[a1], samples[b1]);
							vertices[vertices.Length - 3] = InterpolateVerts(samples[a2], samples[b2]);
						}
					}
				}
			}
		}

		private unsafe void SIMDChunkSizeZ32(int xStart, int xStop)
		{
			if (Chunk.ChunkSizeZ != 32)
				throw new System.Exception("ChunkSize Z must be equal to 32 to use this function");

			xStart = math.clamp(xStart, 0, Chunk.ChunkSizeX - 1);
			xStop = math.clamp(xStop, 0, Chunk.ChunkSizeX - 1);
			if (xStart >= xStop)
				return;

			sbyte* samplesBase = stackalloc sbyte[Chunk.ChunkSizeZ * 4];
			sbyte* samples01 = samplesBase + Chunk.ChunkSizeZ * 0;
			sbyte* samples23 = samplesBase + Chunk.ChunkSizeZ * 2;

			int signBits0, signBits1, signBits2, signBits3;

			float4* samples = stackalloc float4[8];

			for (int x = xStart; x < xStop; x++)
			{
				(signBits2, signBits3) = SimdExtractBitsAndSamples(samples23, volume.GetUnsafeReadOnlyPtr(), x);

				for (int y = 0; y < Chunk.ChunkSizeY - 1; y++)
				{
					// reuse previous step
					var temp = samples01;
					samples01 = samples23;
					samples23 = temp;
					signBits0 = signBits2;
					signBits1 = signBits3;


					(signBits2, signBits3) = SimdExtractBitsAndSamples(samples23, volume.GetUnsafeReadOnlyPtr(), x, y);


					v128 signBits = new v128(signBits0, signBits1, signBits2, signBits3);


					if (SameSigns(signBits))
						continue;


					// make sure there is enought capacity for vertices,
					// otherwise, allocate bit more
					if (vertices.Capacity < vertices.Length + 32 * 5)
						vertices.Capacity = vertices.Length + 128 * 5;
					var verticesPtr = (float3*)vertices.GetUnsafePtr();


					int cornerMask = X86.Sse.movemask_ps(signBits) << 4;

					for (int z = 0; z < Chunk.ChunkSizeZ - 1; z++)
					{
						cornerMask = cornerMask >> 4;
						signBits = X86.Sse2.slli_epi32(signBits, 1);
						cornerMask |= X86.Sse.movemask_ps(signBits) << 4;

						if (cornerMask == 0 || cornerMask == 255)
							continue;


						var zz = z + z;
						samples[0] = new float4(x + 0, y + 0, z + 0, samples01[zz + 0]);
						samples[1] = new float4(x + 1, y + 0, z + 0, samples01[zz + 1]);
						samples[2] = new float4(x + 1, y + 0, z + 1, samples01[zz + 3]);
						samples[3] = new float4(x + 0, y + 0, z + 1, samples01[zz + 2]);
						samples[4] = new float4(x + 0, y + 1, z + 0, samples23[zz + 0]);
						samples[5] = new float4(x + 1, y + 1, z + 0, samples23[zz + 1]);
						samples[6] = new float4(x + 1, y + 1, z + 1, samples23[zz + 3]);
						samples[7] = new float4(x + 0, y + 1, z + 1, samples23[zz + 2]);

						var triangulationTableIndex = cornerMask * Mesher.TriangulationSubTableLenght;

						bounds.item.Encapsulate(new Vector3(x, y, z));
						bounds.item.Encapsulate(new Vector3(x + 1, y + 1, z + 1));

						for (; triangulationTable[triangulationTableIndex] != 99; triangulationTableIndex += 3)
						{
							vertices.Length = vertices.Length + 3; // make room for 3 vertices
							int a0 = cornerIndexA[triangulationTable[triangulationTableIndex]];
							int b0 = cornerIndexB[triangulationTable[triangulationTableIndex]];
							int a1 = cornerIndexA[triangulationTable[triangulationTableIndex + 1]];
							int b1 = cornerIndexB[triangulationTable[triangulationTableIndex + 1]];
							int a2 = cornerIndexA[triangulationTable[triangulationTableIndex + 2]];
							int b2 = cornerIndexB[triangulationTable[triangulationTableIndex + 2]];
							verticesPtr[vertices.Length - 1] = InterpolateVerts(samples[a0], samples[b0]);
							verticesPtr[vertices.Length - 2] = InterpolateVerts(samples[a1], samples[b1]);
							verticesPtr[vertices.Length - 3] = InterpolateVerts(samples[a2], samples[b2]);
						}
					}
				}
			}
		}


		private static unsafe (int, int) SimdExtractBitsAndSamples(sbyte* samples23, void* volumePtr, int x, int y = -1 /* first step */)
		{
			v128 shuffleReverseByteOrder = new v128(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);

			var ptr2 = (sbyte*)volumePtr + (x + 0) * Chunk.ChunkSizeY * Chunk.ChunkSizeZ + (y + 1) * Chunk.ChunkSizeZ;
			var ptr3 = (sbyte*)volumePtr + (x + 1) * Chunk.ChunkSizeY * Chunk.ChunkSizeZ + (y + 1) * Chunk.ChunkSizeZ;
			v128 lo2 = X86.Sse2.load_si128(ptr2 + 0);
			v128 hi2 = X86.Sse2.load_si128(ptr2 + 16);
			v128 lo3 = X86.Sse2.load_si128(ptr3 + 0);
			v128 hi3 = X86.Sse2.load_si128(ptr3 + 16);
			X86.Sse2.store_si128(samples23 + 00, X86.Sse2.unpacklo_epi8(lo2, lo3));
			X86.Sse2.store_si128(samples23 + 16, X86.Sse2.unpackhi_epi8(lo2, lo3));
			X86.Sse2.store_si128(samples23 + 32, X86.Sse2.unpacklo_epi8(hi2, hi3));
			X86.Sse2.store_si128(samples23 + 48, X86.Sse2.unpackhi_epi8(hi2, hi3));
			lo2 = X86.Ssse3.shuffle_epi8(lo2, shuffleReverseByteOrder);
			lo3 = X86.Ssse3.shuffle_epi8(lo3, shuffleReverseByteOrder);
			hi2 = X86.Ssse3.shuffle_epi8(hi2, shuffleReverseByteOrder);
			hi3 = X86.Ssse3.shuffle_epi8(hi3, shuffleReverseByteOrder);
			var signBits2 = (X86.Sse2.movemask_epi8(lo2) << 16 | (X86.Sse2.movemask_epi8(hi2)));
			var signBits3 = (X86.Sse2.movemask_epi8(lo3) << 16 | (X86.Sse2.movemask_epi8(hi3)));
			return (signBits2, signBits3);
		}

		private static bool SameSigns(v128 signBits)
		{
			var maskAllOnes = new v128(uint.MaxValue);
			return X86.Sse4_1.test_mix_ones_zeroes(signBits, maskAllOnes) == 0;
		}



		private static int FlattenIndex(int x, int y, int z) => x * Chunk.ChunkSizeY * Chunk.ChunkSizeZ + y * Chunk.ChunkSizeZ + z;

		private static float3 InterpolateVerts(float4 v1, float4 v2)
		{
			// both version, simd and default assumes that the isolevel is equal to 0
			// its easy to change this in default version (those 8 if's)
			// but simd version extract sign bits, so it might be harder to implement different isolevels.
			const float isoLevel = 0.0f;
			float t = (isoLevel - v1.w) / (v2.w - v1.w);
			return (v1 + t * (v2 - v1)).xyz;
		}



		// bad idea :(
		private static float3 GetInterpolatedVertex(int edge, float sample0, float sample1)
		{
			var t = (-sample0) / (sample1 - sample0);
			switch (edge)
			{
				case 0: return new float3(t, 0, 0);
				case 1: return new float3(1, 0, t);
				case 2: return new float3(1 - t, 0, 1);
				case 3: return new float3(0, 0, 1 - t);

				case 4: return new float3(t, 1, 0);
				case 5: return new float3(1, 1, t);
				case 6: return new float3(1 - t, 1, 1);
				case 7: return new float3(0, 1, 1 - t);

				case 8: return new float3(0, t, 0);
				case 9: return new float3(1, t, 0);
				case 10: return new float3(1, t, 1);
				case 11: return new float3(0, t, 1);

			}
			return default;
		}
	}
}