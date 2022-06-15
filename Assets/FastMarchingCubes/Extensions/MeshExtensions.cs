using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class MeshExtensions
{
	static MeshUpdateFlags updateFlags =
		MeshUpdateFlags.DontNotifyMeshUsers |	
		MeshUpdateFlags.DontRecalculateBounds |	
		MeshUpdateFlags.DontResetBoneBounds |	
		MeshUpdateFlags.DontValidateIndices;	
	
	public static void SetMesh(this Mesh mesh, MarchingCubes.Mesher mesher)
	{
		var vertices = mesher.Vertices;
	
		if (vertices.Length > 2)
		{
			bool use32bitIndices = vertices.Length > ushort.MaxValue;

			if (use32bitIndices)
			{
				var indices = MarchingCubes.Mesher.GetIndices(vertices.Length);
				mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
				mesh.SetIndexBufferData(indices, 0, 0, indices.Length, updateFlags);
			}
			else
			{
				var indices = MarchingCubes.Mesher.GetIndices16(vertices.Length);
				mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt16);
				mesh.SetIndexBufferData(indices, 0, 0, indices.Length, updateFlags);
			}

			mesh.SetVertexBufferParams(vertices.Length, MarchingCubes.Mesher.VertexFormat);
			mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length, 0, updateFlags);
			var subMeshDescriptor = new SubMeshDescriptor(0, vertices.Length, MeshTopology.Triangles);
			mesh.subMeshCount = 1;
			mesh.SetSubMesh(0, subMeshDescriptor, updateFlags);
	
			mesh.bounds = mesher.Bounds;
		}
		else
		{
			mesh.Clear();
		}
	}

	public static void SetMesh(this Mesh mesh, List<MarchingCubes.Mesher> meshers, bool combineMeshesOnCpu)
	{
		var verticesCount = 0;

		if (combineMeshesOnCpu)
		{
			var mesher = meshers[0];
			mesher.CombineMeshers(meshers);
			mesh.SetMesh(mesher);
			return;
		}

		foreach (var mesher in meshers)
		{
			verticesCount += mesher.Vertices.Length;
		}
		
		if (verticesCount > 2)
		{
			bool use32bitIndices = verticesCount > ushort.MaxValue;

			if (use32bitIndices)
			{
				var indices = MarchingCubes.Mesher.GetIndices(verticesCount);
				mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
				mesh.SetIndexBufferData(indices, 0, 0, indices.Length, updateFlags);
			}
			else
			{
				var indices = MarchingCubes.Mesher.GetIndices16(verticesCount);
				mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt16);
				mesh.SetIndexBufferData(indices, 0, 0, indices.Length, updateFlags);
			}

			mesh.SetVertexBufferParams(verticesCount, MarchingCubes.Mesher.VertexFormat);
			var subMeshDescriptor = new SubMeshDescriptor(0, verticesCount, MeshTopology.Triangles);
			mesh.subMeshCount = 1;
			mesh.SetSubMesh(0, subMeshDescriptor, updateFlags);
		
			Bounds bounds = default;
			var previousVerticesCount = 0;
		
			foreach (var mesher in meshers)
			{
				var vertices = mesher.Vertices;
		
				if (vertices.Length > 2)
				{
					mesh.SetVertexBufferData(vertices, 0, previousVerticesCount, vertices.Length, 0, updateFlags);
					bounds.Encapsulate(mesher.Bounds);
					previousVerticesCount += vertices.Length;
				}
			}
		
			mesh.bounds = bounds;
		}
		else
		{
			mesh.Clear();
		}
	}
}
