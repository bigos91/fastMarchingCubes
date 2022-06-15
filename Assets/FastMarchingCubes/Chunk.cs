using System;
using Unity.Collections;

namespace MarchingCubes
{
	public class Chunk : IDisposable
	{
		public const int ChunkSizeX = 32; /* can be changed */
		public const int ChunkSizeY = 32; /* can be changed */
		public const int ChunkSizeZ = 32; /* can be changed , but optimized version of marching cubes would not work (need 32 voxel in z coordinate) */
		public const int VoxelsAmount = ChunkSizeX * ChunkSizeY * ChunkSizeZ;

		public NativeArray<sbyte> data;

		public Chunk()
		{
			data = new NativeArray<sbyte>(ChunkSizeX * ChunkSizeY * ChunkSizeZ, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		}

		public void Dispose()
		{
			data.Dispose();
		}
	}
}