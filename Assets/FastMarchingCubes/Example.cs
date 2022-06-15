using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Profiling;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using WireframeImageEffect = SuperSystems.ImageEffects.WireframeImageEffect;

namespace MarchingCubes
{
	public class Example : MonoBehaviour
	{
		private bool regenerateOnce = true;
		private GenerateJob.Mode generateModePrev;
		public GenerateJob.Mode generateMode;
		public Mesher.Mode meshingMode;
		public int threadCount = 4;

		public bool regenerateChunk = true;
		public float noiseSpeed = 0.1f;
		public GameObject chunkGameObject;
		private MeshFilter chunkMeshFilter;

		Chunk chunk;
		Mesher mesher;
		List<Mesher> meshers = new List<Mesher>(); // for multithreaded meshing
		GenerateJob generateJob;

		TimeCounter meshingCounter = new TimeCounter(samplesCount: 300);
		TimeCounter uploadingCounter = new TimeCounter();
		TimeCounter chunkRegenCounter = new TimeCounter();


		void OnGUI()
		{
			GUILayout.BeginHorizontal();

			{
				var wires = Camera.main.GetComponent<WireframeImageEffect>();

				GUILayout.BeginVertical(GUI.skin.box);
				
				GUILayout.Label("Chunk regenerate mean time: " + chunkRegenCounter.mean.ToString("F3") + " ms");
				GUILayout.Label("Meshing mean time: " + meshingCounter.mean.ToString("F3") + " ms");
				GUILayout.Label("Upload mean time: " + uploadingCounter.mean.ToString("F3") + " ms");
				GUILayout.BeginHorizontal();
				GUILayout.Label("Speed");
				noiseSpeed = GUILayout.HorizontalSlider(noiseSpeed, 0.0f, 2.0f);
				GUILayout.EndHorizontal();
				GUILayout.Space(10);
				wires.wireframeType = GUILayout.Toggle(wires.wireframeType == WireframeImageEffect.WireframeType.Solid, "Wireframe") ? WireframeImageEffect.WireframeType.Solid : WireframeImageEffect.WireframeType.None;

				GUILayout.Space(10);
				GUIToggles();

				GUILayout.Space(10);
				GUILayout.BeginHorizontal();
				GUILayout.Label("Threads " + threadCount);
				threadCount = (int)GUILayout.HorizontalSlider(threadCount, 1, 16);
				GUILayout.EndHorizontal();

				GUILayout.EndVertical();
			}
			{
				GUILayout.BeginVertical(GUI.skin.box);
				GUILayout.Label("Scrool to zoom, RMB to rotate (shift for faster)");
				GUILayout.Label("Vertices " + chunkMeshFilter.mesh.vertexCount);
				GUILayout.EndVertical();
			}

			GUILayout.EndHorizontal();
		}
		void GUIToggles()
		{
			generateMode = GUILayout.Toggle(generateMode == GenerateJob.Mode.SingleSphere, "Single sphere") ? GenerateJob.Mode.SingleSphere : generateMode;
			generateMode = GUILayout.Toggle(generateMode == GenerateJob.Mode.Noise, "Noise") ? GenerateJob.Mode.Noise : generateMode;
			generateMode = GUILayout.Toggle(generateMode == GenerateJob.Mode.Sphereblob, "Sphereblobs") ? GenerateJob.Mode.Sphereblob : generateMode;
			generateMode = GUILayout.Toggle(generateMode == GenerateJob.Mode.Terrain, "Terrain") ? GenerateJob.Mode.Terrain : generateMode;
			GUILayout.Space(5);
			meshingMode = GUILayout.Toggle(meshingMode == Mesher.Mode.Naive, "Naive meshing") ? Mesher.Mode.Naive : meshingMode;
			meshingMode = GUILayout.Toggle(meshingMode == Mesher.Mode.Simd32, "SIMD") ? Mesher.Mode.Simd32 : meshingMode;
			meshingMode = GUILayout.Toggle(meshingMode == Mesher.Mode.Simd32Multithreaded, "SIMD+Threads") ? Mesher.Mode.Simd32Multithreaded : meshingMode;
		}

		void Start()
		{
			threadCount = Mathf.Clamp(threadCount, 1, 16);

			if (chunkGameObject != null)
				chunkMeshFilter = chunkGameObject.GetComponent<MeshFilter>();

			chunk = new Chunk();
			mesher = new Mesher();
			for (int i = 0; i < 16; i++)
				meshers.Add(new Mesher());

			PrepareJobsData();
		}

		void PrepareJobsData()
		{
			generateJob = new GenerateJob
			{
				volume = chunk.data,

				sphereCenter = new float3(15.5f, 15.5f, 15.5f),
				noiseFreq = 0.07f,
				spheresPositions = new NativeArray<float3>(50, Allocator.Persistent),
				spheresDeltas = new NativeArray<float4>(50, Allocator.Persistent)
			};

			for (int i = 0; i < generateJob.spheresPositions.Length; i++)
			{
				generateJob.spheresPositions[i] = new float3
				{
					x = UnityEngine.Random.value * (Chunk.ChunkSizeX - 10) + 5,
					y = UnityEngine.Random.value * (Chunk.ChunkSizeY - 10) + 5,
					z = UnityEngine.Random.value * (Chunk.ChunkSizeZ - 10) + 5,
				};
			}
		}

		void Update()
		{
			if (generateMode != generateModePrev)
			{
				generateModePrev = generateMode;
				regenerateOnce = true;
			}

			if (regenerateChunk || regenerateOnce)
			{
				regenerateOnce = false;
				chunkRegenCounter.Start();
				generateJob.time += Time.deltaTime * noiseSpeed;
				generateJob.mode = generateMode;
				if (generateMode == GenerateJob.Mode.Sphereblob)
					generateJob.Run();
				generateJob.Schedule(32, 1).Complete();
				chunkRegenCounter.Stop();
			}

			if (meshingMode == Mesher.Mode.Simd32Multithreaded)
			{
				List<Mesher> usedMeshers = new List<Mesher>();

				meshingCounter.Start();

				for (int i = 0; i < threadCount; i++)
					usedMeshers.Add(meshers[i]);

				StartMeshGroup(usedMeshers, chunk);

				for (int i = 0; i < usedMeshers.Count; i++)
					usedMeshers[i].WaitForMeshJob();

				meshingCounter.Stop();

				uploadingCounter.Start();
				chunkMeshFilter.mesh.SetMesh(usedMeshers, combineMeshesOnCpu: false);
				uploadingCounter.Stop();
			}
			else
			{
				meshingCounter.Start();
				mesher.StartMeshJob(chunk, meshingMode);
				mesher.WaitForMeshJob();
				meshingCounter.Stop();

				uploadingCounter.Start();
				chunkMeshFilter.mesh.SetMesh(mesher);
				uploadingCounter.Stop();
			}
		}

		void OnDestroy()
		{
			chunk.Dispose();
			mesher.Dispose();
			generateJob.spheresPositions.Dispose();
			generateJob.spheresDeltas.Dispose();

			foreach (var mesher in meshers)
				mesher.Dispose();
		}

		void StartMeshGroup(List<Mesher> meshers, Chunk chunk)
		{
			var range = (Chunk.ChunkSizeX - 1) / meshers.Count;
			var remainder = (Chunk.ChunkSizeX - 1) % meshers.Count;
			var stop = 0;

			for (int i = 0; i < meshers.Count; i++)
			{
				var start = stop;
				stop += range + (remainder-- > 0 ? 1 : 0);
				meshers[i].StartMeshJob(chunk, meshingMode, start, stop);
			}
		}
	}
}
