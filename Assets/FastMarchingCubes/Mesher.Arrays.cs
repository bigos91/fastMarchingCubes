using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace MarchingCubes
{
	public partial class Mesher
	{
		public const int TriangulationSubTableLenght = 16; // can be either 13 or 16, there are 2 different arrays
		// 13 version can produce holes in mesh in some cases.

		private static void AllocateLookupArrays()
		{
			if (triangulationTable.IsCreated)
				return;
			if (TriangulationSubTableLenght != 13 && TriangulationSubTableLenght != 16)
				throw new System.Exception("TriangulationSubTableLenght must be equal 13 or 16");



			// source:
			// http://paulbourke.net/geometry/polygonise/
			//
			// flat array [256][16]
			byte[] triangulationArray = new byte[]{ 
			 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  0,  8,  3, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  0,  1,  9, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  1,  8,  3,  9,  8,  1, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  1,  2, 10, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  0,  8,  3,  1,  2, 10, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  9,  2, 10,  0,  2,  9, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  2,  8,  3,  2, 10,  8, 10,  9,  8, 99, 99, 99, 99, 99, 99, 99,
			  3, 11,  2, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  0, 11,  2,  8, 11,  0, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  1,  9,  0,  2,  3, 11, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  1, 11,  2,  1,  9, 11,  9,  8, 11, 99, 99, 99, 99, 99, 99, 99,
			  3, 10,  1, 11, 10,  3, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  0, 10,  1,  0,  8, 10,  8, 11, 10, 99, 99, 99, 99, 99, 99, 99,
			  3,  9,  0,  3, 11,  9, 11, 10,  9, 99, 99, 99, 99, 99, 99, 99,
			  9,  8, 10, 10,  8, 11, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  4,  7,  8, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  4,  3,  0,  7,  3,  4, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  0,  1,  9,  8,  4,  7, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  4,  1,  9,  4,  7,  1,  7,  3,  1, 99, 99, 99, 99, 99, 99, 99,
			  1,  2, 10,  8,  4,  7, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  3,  4,  7,  3,  0,  4,  1,  2, 10, 99, 99, 99, 99, 99, 99, 99,
			  9,  2, 10,  9,  0,  2,  8,  4,  7, 99, 99, 99, 99, 99, 99, 99,
			  2, 10,  9,  2,  9,  7,  2,  7,  3,  7,  9,  4, 99, 99, 99, 99,
			  8,  4,  7,  3, 11,  2, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			 11,  4,  7, 11,  2,  4,  2,  0,  4, 99, 99, 99, 99, 99, 99, 99,
			  9,  0,  1,  8,  4,  7,  2,  3, 11, 99, 99, 99, 99, 99, 99, 99,
			  4,  7, 11,  9,  4, 11,  9, 11,  2,  9,  2,  1, 99, 99, 99, 99,
			  3, 10,  1,  3, 11, 10,  7,  8,  4, 99, 99, 99, 99, 99, 99, 99,
			  1, 11, 10,  1,  4, 11,  1,  0,  4,  7, 11,  4, 99, 99, 99, 99,
			  4,  7,  8,  9,  0, 11,  9, 11, 10, 11,  0,  3, 99, 99, 99, 99,
			  4,  7, 11,  4, 11,  9,  9, 11, 10, 99, 99, 99, 99, 99, 99, 99,
			  9,  5,  4, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  9,  5,  4,  0,  8,  3, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  0,  5,  4,  1,  5,  0, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  8,  5,  4,  8,  3,  5,  3,  1,  5, 99, 99, 99, 99, 99, 99, 99,
			  1,  2, 10,  9,  5,  4, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  3,  0,  8,  1,  2, 10,  4,  9,  5, 99, 99, 99, 99, 99, 99, 99,
			  5,  2, 10,  5,  4,  2,  4,  0,  2, 99, 99, 99, 99, 99, 99, 99,
			  2, 10,  5,  3,  2,  5,  3,  5,  4,  3,  4,  8, 99, 99, 99, 99,
			  9,  5,  4,  2,  3, 11, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  0, 11,  2,  0,  8, 11,  4,  9,  5, 99, 99, 99, 99, 99, 99, 99,
			  0,  5,  4,  0,  1,  5,  2,  3, 11, 99, 99, 99, 99, 99, 99, 99,
			  2,  1,  5,  2,  5,  8,  2,  8, 11,  4,  8,  5, 99, 99, 99, 99,
			 10,  3, 11, 10,  1,  3,  9,  5,  4, 99, 99, 99, 99, 99, 99, 99,
			  4,  9,  5,  0,  8,  1,  8, 10,  1,  8, 11, 10, 99, 99, 99, 99,
			  5,  4,  0,  5,  0, 11,  5, 11, 10, 11,  0,  3, 99, 99, 99, 99,
			  5,  4,  8,  5,  8, 10, 10,  8, 11, 99, 99, 99, 99, 99, 99, 99,
			  9,  7,  8,  5,  7,  9, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  9,  3,  0,  9,  5,  3,  5,  7,  3, 99, 99, 99, 99, 99, 99, 99,
			  0,  7,  8,  0,  1,  7,  1,  5,  7, 99, 99, 99, 99, 99, 99, 99,
			  1,  5,  3,  3,  5,  7, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  9,  7,  8,  9,  5,  7, 10,  1,  2, 99, 99, 99, 99, 99, 99, 99,
			 10,  1,  2,  9,  5,  0,  5,  3,  0,  5,  7,  3, 99, 99, 99, 99,
			  8,  0,  2,  8,  2,  5,  8,  5,  7, 10,  5,  2, 99, 99, 99, 99,
			  2, 10,  5,  2,  5,  3,  3,  5,  7, 99, 99, 99, 99, 99, 99, 99,
			  7,  9,  5,  7,  8,  9,  3, 11,  2, 99, 99, 99, 99, 99, 99, 99,
			  9,  5,  7,  9,  7,  2,  9,  2,  0,  2,  7, 11, 99, 99, 99, 99,
			  2,  3, 11,  0,  1,  8,  1,  7,  8,  1,  5,  7, 99, 99, 99, 99,
			 11,  2,  1, 11,  1,  7,  7,  1,  5, 99, 99, 99, 99, 99, 99, 99,
			  9,  5,  8,  8,  5,  7, 10,  1,  3, 10,  3, 11, 99, 99, 99, 99,
			  5,  7,  0,  5,  0,  9,  7, 11,  0,  1,  0, 10, 11, 10,  0, 99,
			 11, 10,  0, 11,  0,  3, 10,  5,  0,  8,  0,  7,  5,  7,  0, 99,
			 11, 10,  5,  7, 11,  5, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			 10,  6,  5, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  0,  8,  3,  5, 10,  6, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  9,  0,  1,  5, 10,  6, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  1,  8,  3,  1,  9,  8,  5, 10,  6, 99, 99, 99, 99, 99, 99, 99,
			  1,  6,  5,  2,  6,  1, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  1,  6,  5,  1,  2,  6,  3,  0,  8, 99, 99, 99, 99, 99, 99, 99,
			  9,  6,  5,  9,  0,  6,  0,  2,  6, 99, 99, 99, 99, 99, 99, 99,
			  5,  9,  8,  5,  8,  2,  5,  2,  6,  3,  2,  8, 99, 99, 99, 99,
			  2,  3, 11, 10,  6,  5, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			 11,  0,  8, 11,  2,  0, 10,  6,  5, 99, 99, 99, 99, 99, 99, 99,
			  0,  1,  9,  2,  3, 11,  5, 10,  6, 99, 99, 99, 99, 99, 99, 99,
			  5, 10,  6,  1,  9,  2,  9, 11,  2,  9,  8, 11, 99, 99, 99, 99,
			  6,  3, 11,  6,  5,  3,  5,  1,  3, 99, 99, 99, 99, 99, 99, 99,
			  0,  8, 11,  0, 11,  5,  0,  5,  1,  5, 11,  6, 99, 99, 99, 99,
			  3, 11,  6,  0,  3,  6,  0,  6,  5,  0,  5,  9, 99, 99, 99, 99,
			  6,  5,  9,  6,  9, 11, 11,  9,  8, 99, 99, 99, 99, 99, 99, 99,
			  5, 10,  6,  4,  7,  8, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  4,  3,  0,  4,  7,  3,  6,  5, 10, 99, 99, 99, 99, 99, 99, 99,
			  1,  9,  0,  5, 10,  6,  8,  4,  7, 99, 99, 99, 99, 99, 99, 99,
			 10,  6,  5,  1,  9,  7,  1,  7,  3,  7,  9,  4, 99, 99, 99, 99,
			  6,  1,  2,  6,  5,  1,  4,  7,  8, 99, 99, 99, 99, 99, 99, 99,
			  1,  2,  5,  5,  2,  6,  3,  0,  4,  3,  4,  7, 99, 99, 99, 99,
			  8,  4,  7,  9,  0,  5,  0,  6,  5,  0,  2,  6, 99, 99, 99, 99,
			  7,  3,  9,  7,  9,  4,  3,  2,  9,  5,  9,  6,  2,  6,  9, 99,
			  3, 11,  2,  7,  8,  4, 10,  6,  5, 99, 99, 99, 99, 99, 99, 99,
			  5, 10,  6,  4,  7,  2,  4,  2,  0,  2,  7, 11, 99, 99, 99, 99,
			  0,  1,  9,  4,  7,  8,  2,  3, 11,  5, 10,  6, 99, 99, 99, 99,
			  9,  2,  1,  9, 11,  2,  9,  4, 11,  7, 11,  4,  5, 10,  6, 99,
			  8,  4,  7,  3, 11,  5,  3,  5,  1,  5, 11,  6, 99, 99, 99, 99,
			  5,  1, 11,  5, 11,  6,  1,  0, 11,  7, 11,  4,  0,  4, 11, 99,
			  0,  5,  9,  0,  6,  5,  0,  3,  6, 11,  6,  3,  8,  4,  7, 99,
			  6,  5,  9,  6,  9, 11,  4,  7,  9,  7, 11,  9, 99, 99, 99, 99,
			 10,  4,  9,  6,  4, 10, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  4, 10,  6,  4,  9, 10,  0,  8,  3, 99, 99, 99, 99, 99, 99, 99,
			 10,  0,  1, 10,  6,  0,  6,  4,  0, 99, 99, 99, 99, 99, 99, 99,
			  8,  3,  1,  8,  1,  6,  8,  6,  4,  6,  1, 10, 99, 99, 99, 99,
			  1,  4,  9,  1,  2,  4,  2,  6,  4, 99, 99, 99, 99, 99, 99, 99,
			  3,  0,  8,  1,  2,  9,  2,  4,  9,  2,  6,  4, 99, 99, 99, 99,
			  0,  2,  4,  4,  2,  6, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  8,  3,  2,  8,  2,  4,  4,  2,  6, 99, 99, 99, 99, 99, 99, 99,
			 10,  4,  9, 10,  6,  4, 11,  2,  3, 99, 99, 99, 99, 99, 99, 99,
			  0,  8,  2,  2,  8, 11,  4,  9, 10,  4, 10,  6, 99, 99, 99, 99,
			  3, 11,  2,  0,  1,  6,  0,  6,  4,  6,  1, 10, 99, 99, 99, 99,
			  6,  4,  1,  6,  1, 10,  4,  8,  1,  2,  1, 11,  8, 11,  1, 99,
			  9,  6,  4,  9,  3,  6,  9,  1,  3, 11,  6,  3, 99, 99, 99, 99,
			  8, 11,  1,  8,  1,  0, 11,  6,  1,  9,  1,  4,  6,  4,  1, 99,
			  3, 11,  6,  3,  6,  0,  0,  6,  4, 99, 99, 99, 99, 99, 99, 99,
			  6,  4,  8, 11,  6,  8, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  7, 10,  6,  7,  8, 10,  8,  9, 10, 99, 99, 99, 99, 99, 99, 99,
			  0,  7,  3,  0, 10,  7,  0,  9, 10,  6,  7, 10, 99, 99, 99, 99,
			 10,  6,  7,  1, 10,  7,  1,  7,  8,  1,  8,  0, 99, 99, 99, 99,
			 10,  6,  7, 10,  7,  1,  1,  7,  3, 99, 99, 99, 99, 99, 99, 99,
			  1,  2,  6,  1,  6,  8,  1,  8,  9,  8,  6,  7, 99, 99, 99, 99,
			  2,  6,  9,  2,  9,  1,  6,  7,  9,  0,  9,  3,  7,  3,  9, 99,
			  7,  8,  0,  7,  0,  6,  6,  0,  2, 99, 99, 99, 99, 99, 99, 99,
			  7,  3,  2,  6,  7,  2, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  2,  3, 11, 10,  6,  8, 10,  8,  9,  8,  6,  7, 99, 99, 99, 99,
			  2,  0,  7,  2,  7, 11,  0,  9,  7,  6,  7, 10,  9, 10,  7, 99,
			  1,  8,  0,  1,  7,  8,  1, 10,  7,  6,  7, 10,  2,  3, 11, 99,
			 11,  2,  1, 11,  1,  7, 10,  6,  1,  6,  7,  1, 99, 99, 99, 99,
			  8,  9,  6,  8,  6,  7,  9,  1,  6, 11,  6,  3,  1,  3,  6, 99,
			  0,  9,  1, 11,  6,  7, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  7,  8,  0,  7,  0,  6,  3, 11,  0, 11,  6,  0, 99, 99, 99, 99,
			  7, 11,  6, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  7,  6, 11, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  3,  0,  8, 11,  7,  6, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  0,  1,  9, 11,  7,  6, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  8,  1,  9,  8,  3,  1, 11,  7,  6, 99, 99, 99, 99, 99, 99, 99,
			 10,  1,  2,  6, 11,  7, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  1,  2, 10,  3,  0,  8,  6, 11,  7, 99, 99, 99, 99, 99, 99, 99,
			  2,  9,  0,  2, 10,  9,  6, 11,  7, 99, 99, 99, 99, 99, 99, 99,
			  6, 11,  7,  2, 10,  3, 10,  8,  3, 10,  9,  8, 99, 99, 99, 99,
			  7,  2,  3,  6,  2,  7, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  7,  0,  8,  7,  6,  0,  6,  2,  0, 99, 99, 99, 99, 99, 99, 99,
			  2,  7,  6,  2,  3,  7,  0,  1,  9, 99, 99, 99, 99, 99, 99, 99,
			  1,  6,  2,  1,  8,  6,  1,  9,  8,  8,  7,  6, 99, 99, 99, 99,
			 10,  7,  6, 10,  1,  7,  1,  3,  7, 99, 99, 99, 99, 99, 99, 99,
			 10,  7,  6,  1,  7, 10,  1,  8,  7,  1,  0,  8, 99, 99, 99, 99,
			  0,  3,  7,  0,  7, 10,  0, 10,  9,  6, 10,  7, 99, 99, 99, 99,
			  7,  6, 10,  7, 10,  8,  8, 10,  9, 99, 99, 99, 99, 99, 99, 99,
			  6,  8,  4, 11,  8,  6, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  3,  6, 11,  3,  0,  6,  0,  4,  6, 99, 99, 99, 99, 99, 99, 99,
			  8,  6, 11,  8,  4,  6,  9,  0,  1, 99, 99, 99, 99, 99, 99, 99,
			  9,  4,  6,  9,  6,  3,  9,  3,  1, 11,  3,  6, 99, 99, 99, 99,
			  6,  8,  4,  6, 11,  8,  2, 10,  1, 99, 99, 99, 99, 99, 99, 99,
			  1,  2, 10,  3,  0, 11,  0,  6, 11,  0,  4,  6, 99, 99, 99, 99,
			  4, 11,  8,  4,  6, 11,  0,  2,  9,  2, 10,  9, 99, 99, 99, 99,
			 10,  9,  3, 10,  3,  2,  9,  4,  3, 11,  3,  6,  4,  6,  3, 99,
			  8,  2,  3,  8,  4,  2,  4,  6,  2, 99, 99, 99, 99, 99, 99, 99,
			  0,  4,  2,  4,  6,  2, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  1,  9,  0,  2,  3,  4,  2,  4,  6,  4,  3,  8, 99, 99, 99, 99,
			  1,  9,  4,  1,  4,  2,  2,  4,  6, 99, 99, 99, 99, 99, 99, 99,
			  8,  1,  3,  8,  6,  1,  8,  4,  6,  6, 10,  1, 99, 99, 99, 99,
			 10,  1,  0, 10,  0,  6,  6,  0,  4, 99, 99, 99, 99, 99, 99, 99,
			  4,  6,  3,  4,  3,  8,  6, 10,  3,  0,  3,  9, 10,  9,  3, 99,
			 10,  9,  4,  6, 10,  4, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  4,  9,  5,  7,  6, 11, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  0,  8,  3,  4,  9,  5, 11,  7,  6, 99, 99, 99, 99, 99, 99, 99,
			  5,  0,  1,  5,  4,  0,  7,  6, 11, 99, 99, 99, 99, 99, 99, 99,
			 11,  7,  6,  8,  3,  4,  3,  5,  4,  3,  1,  5, 99, 99, 99, 99,
			  9,  5,  4, 10,  1,  2,  7,  6, 11, 99, 99, 99, 99, 99, 99, 99,
			  6, 11,  7,  1,  2, 10,  0,  8,  3,  4,  9,  5, 99, 99, 99, 99,
			  7,  6, 11,  5,  4, 10,  4,  2, 10,  4,  0,  2, 99, 99, 99, 99,
			  3,  4,  8,  3,  5,  4,  3,  2,  5, 10,  5,  2, 11,  7,  6, 99,
			  7,  2,  3,  7,  6,  2,  5,  4,  9, 99, 99, 99, 99, 99, 99, 99,
			  9,  5,  4,  0,  8,  6,  0,  6,  2,  6,  8,  7, 99, 99, 99, 99,
			  3,  6,  2,  3,  7,  6,  1,  5,  0,  5,  4,  0, 99, 99, 99, 99,
			  6,  2,  8,  6,  8,  7,  2,  1,  8,  4,  8,  5,  1,  5,  8, 99,
			  9,  5,  4, 10,  1,  6,  1,  7,  6,  1,  3,  7, 99, 99, 99, 99,
			  1,  6, 10,  1,  7,  6,  1,  0,  7,  8,  7,  0,  9,  5,  4, 99,
			  4,  0, 10,  4, 10,  5,  0,  3, 10,  6, 10,  7,  3,  7, 10, 99,
			  7,  6, 10,  7, 10,  8,  5,  4, 10,  4,  8, 10, 99, 99, 99, 99,
			  6,  9,  5,  6, 11,  9, 11,  8,  9, 99, 99, 99, 99, 99, 99, 99,
			  3,  6, 11,  0,  6,  3,  0,  5,  6,  0,  9,  5, 99, 99, 99, 99,
			  0, 11,  8,  0,  5, 11,  0,  1,  5,  5,  6, 11, 99, 99, 99, 99,
			  6, 11,  3,  6,  3,  5,  5,  3,  1, 99, 99, 99, 99, 99, 99, 99,
			  1,  2, 10,  9,  5, 11,  9, 11,  8, 11,  5,  6, 99, 99, 99, 99,
			  0, 11,  3,  0,  6, 11,  0,  9,  6,  5,  6,  9,  1,  2, 10, 99,
			 11,  8,  5, 11,  5,  6,  8,  0,  5, 10,  5,  2,  0,  2,  5, 99,
			  6, 11,  3,  6,  3,  5,  2, 10,  3, 10,  5,  3, 99, 99, 99, 99,
			  5,  8,  9,  5,  2,  8,  5,  6,  2,  3,  8,  2, 99, 99, 99, 99,
			  9,  5,  6,  9,  6,  0,  0,  6,  2, 99, 99, 99, 99, 99, 99, 99,
			  1,  5,  8,  1,  8,  0,  5,  6,  8,  3,  8,  2,  6,  2,  8, 99,
			  1,  5,  6,  2,  1,  6, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  1,  3,  6,  1,  6, 10,  3,  8,  6,  5,  6,  9,  8,  9,  6, 99,
			 10,  1,  0, 10,  0,  6,  9,  5,  0,  5,  6,  0, 99, 99, 99, 99,
			  0,  3,  8,  5,  6, 10, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			 10,  5,  6, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			 11,  5, 10,  7,  5, 11, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			 11,  5, 10, 11,  7,  5,  8,  3,  0, 99, 99, 99, 99, 99, 99, 99,
			  5, 11,  7,  5, 10, 11,  1,  9,  0, 99, 99, 99, 99, 99, 99, 99,
			 10,  7,  5, 10, 11,  7,  9,  8,  1,  8,  3,  1, 99, 99, 99, 99,
			 11,  1,  2, 11,  7,  1,  7,  5,  1, 99, 99, 99, 99, 99, 99, 99,
			  0,  8,  3,  1,  2,  7,  1,  7,  5,  7,  2, 11, 99, 99, 99, 99,
			  9,  7,  5,  9,  2,  7,  9,  0,  2,  2, 11,  7, 99, 99, 99, 99,
			  7,  5,  2,  7,  2, 11,  5,  9,  2,  3,  2,  8,  9,  8,  2, 99,
			  2,  5, 10,  2,  3,  5,  3,  7,  5, 99, 99, 99, 99, 99, 99, 99,
			  8,  2,  0,  8,  5,  2,  8,  7,  5, 10,  2,  5, 99, 99, 99, 99,
			  9,  0,  1,  5, 10,  3,  5,  3,  7,  3, 10,  2, 99, 99, 99, 99,
			  9,  8,  2,  9,  2,  1,  8,  7,  2, 10,  2,  5,  7,  5,  2, 99,
			  1,  3,  5,  3,  7,  5, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  0,  8,  7,  0,  7,  1,  1,  7,  5, 99, 99, 99, 99, 99, 99, 99,
			  9,  0,  3,  9,  3,  5,  5,  3,  7, 99, 99, 99, 99, 99, 99, 99,
			  9,  8,  7,  5,  9,  7, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  5,  8,  4,  5, 10,  8, 10, 11,  8, 99, 99, 99, 99, 99, 99, 99,
			  5,  0,  4,  5, 11,  0,  5, 10, 11, 11,  3,  0, 99, 99, 99, 99,
			  0,  1,  9,  8,  4, 10,  8, 10, 11, 10,  4,  5, 99, 99, 99, 99,
			 10, 11,  4, 10,  4,  5, 11,  3,  4,  9,  4,  1,  3,  1,  4, 99,
			  2,  5,  1,  2,  8,  5,  2, 11,  8,  4,  5,  8, 99, 99, 99, 99,
			  0,  4, 11,  0, 11,  3,  4,  5, 11,  2, 11,  1,  5,  1, 11, 99,
			  0,  2,  5,  0,  5,  9,  2, 11,  5,  4,  5,  8, 11,  8,  5, 99,
			  9,  4,  5,  2, 11,  3, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  2,  5, 10,  3,  5,  2,  3,  4,  5,  3,  8,  4, 99, 99, 99, 99,
			  5, 10,  2,  5,  2,  4,  4,  2,  0, 99, 99, 99, 99, 99, 99, 99,
			  3, 10,  2,  3,  5, 10,  3,  8,  5,  4,  5,  8,  0,  1,  9, 99,
			  5, 10,  2,  5,  2,  4,  1,  9,  2,  9,  4,  2, 99, 99, 99, 99,
			  8,  4,  5,  8,  5,  3,  3,  5,  1, 99, 99, 99, 99, 99, 99, 99,
			  0,  4,  5,  1,  0,  5, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  8,  4,  5,  8,  5,  3,  9,  0,  5,  0,  3,  5, 99, 99, 99, 99,
			  9,  4,  5, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  4, 11,  7,  4,  9, 11,  9, 10, 11, 99, 99, 99, 99, 99, 99, 99,
			  0,  8,  3,  4,  9,  7,  9, 11,  7,  9, 10, 11, 99, 99, 99, 99,
			  1, 10, 11,  1, 11,  4,  1,  4,  0,  7,  4, 11, 99, 99, 99, 99,
			  3,  1,  4,  3,  4,  8,  1, 10,  4,  7,  4, 11, 10, 11,  4, 99,
			  4, 11,  7,  9, 11,  4,  9,  2, 11,  9,  1,  2, 99, 99, 99, 99,
			  9,  7,  4,  9, 11,  7,  9,  1, 11,  2, 11,  1,  0,  8,  3, 99,
			 11,  7,  4, 11,  4,  2,  2,  4,  0, 99, 99, 99, 99, 99, 99, 99,
			 11,  7,  4, 11,  4,  2,  8,  3,  4,  3,  2,  4, 99, 99, 99, 99,
			  2,  9, 10,  2,  7,  9,  2,  3,  7,  7,  4,  9, 99, 99, 99, 99,
			  9, 10,  7,  9,  7,  4, 10,  2,  7,  8,  7,  0,  2,  0,  7, 99,
			  3,  7, 10,  3, 10,  2,  7,  4, 10,  1, 10,  0,  4,  0, 10, 99,
			  1, 10,  2,  8,  7,  4, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  4,  9,  1,  4,  1,  7,  7,  1,  3, 99, 99, 99, 99, 99, 99, 99,
			  4,  9,  1,  4,  1,  7,  0,  8,  1,  8,  7,  1, 99, 99, 99, 99,
			  4,  0,  3,  7,  4,  3, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  4,  8,  7, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  9, 10,  8, 10, 11,  8, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  3,  0,  9,  3,  9, 11, 11,  9, 10, 99, 99, 99, 99, 99, 99, 99,
			  0,  1, 10,  0, 10,  8,  8, 10, 11, 99, 99, 99, 99, 99, 99, 99,
			  3,  1, 10, 11,  3, 10, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  1,  2, 11,  1, 11,  9,  9, 11,  8, 99, 99, 99, 99, 99, 99, 99,
			  3,  0,  9,  3,  9, 11,  1,  2,  9,  2, 11,  9, 99, 99, 99, 99,
			  0,  2, 11,  8,  0, 11, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  3,  2, 11, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  2,  3,  8,  2,  8, 10, 10,  8,  9, 99, 99, 99, 99, 99, 99, 99,
			  9, 10,  2,  0,  9,  2, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  2,  3,  8,  2,  8, 10,  0,  1,  8,  1, 10,  8, 99, 99, 99, 99,
			  1, 10,  2, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  1,  3,  8,  9,  1,  8, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  0,  9,  1, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			  0,  3,  8, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99};

			// source:
			// http://paulbourke.net/geometry/polygonise/table2.txt
			//
			// flat array [256][13]
			byte[] triangulationArrayAlternative = new byte[]
			{
				99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				 8,  3,  0, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				 9,  0,  1, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				 8,  3,  1,  8,  1,  9, 99, 99, 99, 99, 99, 99, 99,
				10,  1,  2, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				 8,  3,  0,  1,  2, 10, 99, 99, 99, 99, 99, 99, 99,
				 9,  0,  2,  9,  2, 10, 99, 99, 99, 99, 99, 99, 99,
				 3,  2,  8,  2, 10,  8,  8, 10,  9, 99, 99, 99, 99,
				11,  2,  3, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				11,  2,  0, 11,  0,  8, 99, 99, 99, 99, 99, 99, 99,
				11,  2,  3,  0,  1,  9, 99, 99, 99, 99, 99, 99, 99,
				 2,  1, 11,  1,  9, 11, 11,  9,  8, 99, 99, 99, 99,
				10,  1,  3, 10,  3, 11, 99, 99, 99, 99, 99, 99, 99,
				 1,  0, 10,  0,  8, 10, 10,  8, 11, 99, 99, 99, 99,
				 0,  3,  9,  3, 11,  9,  9, 11, 10, 99, 99, 99, 99,
				 8, 10,  9,  8, 11, 10, 99, 99, 99, 99, 99, 99, 99,
				 8,  4,  7, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				 3,  0,  4,  3,  4,  7, 99, 99, 99, 99, 99, 99, 99,
				 1,  9,  0,  8,  4,  7, 99, 99, 99, 99, 99, 99, 99,
				 9,  4,  1,  4,  7,  1,  1,  7,  3, 99, 99, 99, 99,
				10,  1,  2,  8,  4,  7, 99, 99, 99, 99, 99, 99, 99,
				 2, 10,  1,  0,  4,  7,  0,  7,  3, 99, 99, 99, 99,
				 4,  7,  8,  0,  2, 10,  0, 10,  9, 99, 99, 99, 99,
				 2,  7,  3,  2,  9,  7,  7,  9,  4,  2, 10,  9, 99,
				 2,  3, 11,  7,  8,  4, 99, 99, 99, 99, 99, 99, 99,
				 7, 11,  4, 11,  2,  4,  4,  2,  0, 99, 99, 99, 99,
				 3, 11,  2,  4,  7,  8,  9,  0,  1, 99, 99, 99, 99,
				 2,  7, 11,  2,  1,  7,  1,  4,  7,  1,  9,  4, 99,
				 8,  4,  7, 11, 10,  1, 11,  1,  3, 99, 99, 99, 99,
				11,  4,  7,  1,  4, 11,  1, 11, 10,  1,  0,  4, 99,
				 3,  8,  0,  7, 11,  4, 11,  9,  4, 11, 10,  9, 99,
				 7, 11,  4,  4, 11,  9, 11, 10,  9, 99, 99, 99, 99,
				 9,  5,  4, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				 3,  0,  8,  4,  9,  5, 99, 99, 99, 99, 99, 99, 99,
				 5,  4,  0,  5,  0,  1, 99, 99, 99, 99, 99, 99, 99,
				 4,  8,  5,  8,  3,  5,  5,  3,  1, 99, 99, 99, 99,
				 2, 10,  1,  9,  5,  4, 99, 99, 99, 99, 99, 99, 99,
				 0,  8,  3,  5,  4,  9, 10,  1,  2, 99, 99, 99, 99,
				10,  5,  2,  5,  4,  2,  2,  4,  0, 99, 99, 99, 99,
				 3,  4,  8,  3,  2,  4,  2,  5,  4,  2, 10,  5, 99,
				11,  2,  3,  9,  5,  4, 99, 99, 99, 99, 99, 99, 99,
				 9,  5,  4,  8, 11,  2,  8,  2,  0, 99, 99, 99, 99,
				 3, 11,  2,  1,  5,  4,  1,  4,  0, 99, 99, 99, 99,
				 8,  5,  4,  2,  5,  8,  2,  8, 11,  2,  1,  5, 99,
				 5,  4,  9,  1,  3, 11,  1, 11, 10, 99, 99, 99, 99,
				 0,  9,  1,  4,  8,  5,  8, 10,  5,  8, 11, 10, 99,
				 3,  4,  0,  3, 10,  4,  4, 10,  5,  3, 11, 10, 99,
				 4,  8,  5,  5,  8, 10,  8, 11, 10, 99, 99, 99, 99,
				 9,  5,  7,  9,  7,  8, 99, 99, 99, 99, 99, 99, 99,
				 0,  9,  3,  9,  5,  3,  3,  5,  7, 99, 99, 99, 99,
				 8,  0,  7,  0,  1,  7,  7,  1,  5, 99, 99, 99, 99,
				 1,  7,  3,  1,  5,  7, 99, 99, 99, 99, 99, 99, 99,
				 1,  2, 10,  5,  7,  8,  5,  8,  9, 99, 99, 99, 99,
				 9,  1,  0, 10,  5,  2,  5,  3,  2,  5,  7,  3, 99,
				 5,  2, 10,  8,  2,  5,  8,  5,  7,  8,  0,  2, 99,
				10,  5,  2,  2,  5,  3,  5,  7,  3, 99, 99, 99, 99,
				11,  2,  3,  8,  9,  5,  8,  5,  7, 99, 99, 99, 99,
				 9,  2,  0,  9,  7,  2,  2,  7, 11,  9,  5,  7, 99,
				 0,  3,  8,  2,  1, 11,  1,  7, 11,  1,  5,  7, 99,
				 2,  1, 11, 11,  1,  7,  1,  5,  7, 99, 99, 99, 99,
				 3,  9,  1,  3,  8,  9,  7, 11, 10,  7, 10,  5, 99,
				 9,  1,  0, 10,  7, 11, 10,  5,  7, 99, 99, 99, 99,
				 3,  8,  0,  7, 10,  5,  7, 11, 10, 99, 99, 99, 99,
				11,  5,  7, 11, 10,  5, 99, 99, 99, 99, 99, 99, 99,
				10,  6,  5, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				 8,  3,  0, 10,  6,  5, 99, 99, 99, 99, 99, 99, 99,
				 0,  1,  9,  5, 10,  6, 99, 99, 99, 99, 99, 99, 99,
				10,  6,  5,  9,  8,  3,  9,  3,  1, 99, 99, 99, 99,
				 1,  2,  6,  1,  6,  5, 99, 99, 99, 99, 99, 99, 99,
				 0,  8,  3,  2,  6,  5,  2,  5,  1, 99, 99, 99, 99,
				 5,  9,  6,  9,  0,  6,  6,  0,  2, 99, 99, 99, 99,
				 9,  6,  5,  3,  6,  9,  3,  9,  8,  3,  2,  6, 99,
				 3, 11,  2, 10,  6,  5, 99, 99, 99, 99, 99, 99, 99,
				 6,  5, 10,  2,  0,  8,  2,  8, 11, 99, 99, 99, 99,
				 1,  9,  0,  6,  5, 10, 11,  2,  3, 99, 99, 99, 99,
				 1, 10,  2,  5,  9,  6,  9, 11,  6,  9,  8, 11, 99,
				11,  6,  3,  6,  5,  3,  3,  5,  1, 99, 99, 99, 99,
				 0,  5,  1,  0, 11,  5,  5, 11,  6,  0,  8, 11, 99,
				 0,  5,  9,  0,  3,  5,  3,  6,  5,  3, 11,  6, 99,
				 5,  9,  6,  6,  9, 11,  9,  8, 11, 99, 99, 99, 99,
				10,  6,  5,  4,  7,  8, 99, 99, 99, 99, 99, 99, 99,
				 5, 10,  6,  7,  3,  0,  7,  0,  4, 99, 99, 99, 99,
				 5, 10,  6,  0,  1,  9,  8,  4,  7, 99, 99, 99, 99,
				 4,  5,  9,  6,  7, 10,  7,  1, 10,  7,  3,  1, 99,
				 7,  8,  4,  5,  1,  2,  5,  2,  6, 99, 99, 99, 99,
				 4,  1,  0,  4,  5,  1,  6,  7,  3,  6,  3,  2, 99,
				 9,  4,  5,  8,  0,  7,  0,  6,  7,  0,  2,  6, 99,
				 4,  5,  9,  6,  3,  2,  6,  7,  3, 99, 99, 99, 99,
				 7,  8,  4,  2,  3, 11, 10,  6,  5, 99, 99, 99, 99,
				11,  6,  7, 10,  2,  5,  2,  4,  5,  2,  0,  4, 99,
				11,  6,  7,  8,  0,  3,  1, 10,  2,  9,  4,  5, 99,
				 6,  7, 11,  1, 10,  2,  9,  4,  5, 99, 99, 99, 99,
				 6,  7, 11,  4,  5,  8,  5,  3,  8,  5,  1,  3, 99,
				 6,  7, 11,  4,  1,  0,  4,  5,  1, 99, 99, 99, 99,
				 4,  5,  9,  3,  8,  0, 11,  6,  7, 99, 99, 99, 99,
				 9,  4,  5,  7, 11,  6, 99, 99, 99, 99, 99, 99, 99,
				10,  6,  4, 10,  4,  9, 99, 99, 99, 99, 99, 99, 99,
				 8,  3,  0,  9, 10,  6,  9,  6,  4, 99, 99, 99, 99,
				 1, 10,  0, 10,  6,  0,  0,  6,  4, 99, 99, 99, 99,
				 8,  6,  4,  8,  1,  6,  6,  1, 10,  8,  3,  1, 99,
				 9,  1,  4,  1,  2,  4,  4,  2,  6, 99, 99, 99, 99,
				 1,  0,  9,  3,  2,  8,  2,  4,  8,  2,  6,  4, 99,
				 2,  4,  0,  2,  6,  4, 99, 99, 99, 99, 99, 99, 99,
				 3,  2,  8,  8,  2,  4,  2,  6,  4, 99, 99, 99, 99,
				 2,  3, 11,  6,  4,  9,  6,  9, 10, 99, 99, 99, 99,
				 0, 10,  2,  0,  9, 10,  4,  8, 11,  4, 11,  6, 99,
				10,  2,  1, 11,  6,  3,  6,  0,  3,  6,  4,  0, 99,
				10,  2,  1, 11,  4,  8, 11,  6,  4, 99, 99, 99, 99,
				 1,  4,  9, 11,  4,  1, 11,  1,  3, 11,  6,  4, 99,
				 0,  9,  1,  4, 11,  6,  4,  8, 11, 99, 99, 99, 99,
				11,  6,  3,  3,  6,  0,  6,  4,  0, 99, 99, 99, 99,
				 8,  6,  4,  8, 11,  6, 99, 99, 99, 99, 99, 99, 99,
				 6,  7, 10,  7,  8, 10, 10,  8,  9, 99, 99, 99, 99,
				 9,  3,  0,  6,  3,  9,  6,  9, 10,  6,  7,  3, 99,
				 6,  1, 10,  6,  7,  1,  7,  0,  1,  7,  8,  0, 99,
				 6,  7, 10, 10,  7,  1,  7,  3,  1, 99, 99, 99, 99,
				 7,  2,  6,  7,  9,  2,  2,  9,  1,  7,  8,  9, 99,
				 1,  0,  9,  3,  6,  7,  3,  2,  6, 99, 99, 99, 99,
				 8,  0,  7,  7,  0,  6,  0,  2,  6, 99, 99, 99, 99,
				 2,  7,  3,  2,  6,  7, 99, 99, 99, 99, 99, 99, 99,
				 7, 11,  6,  3,  8,  2,  8, 10,  2,  8,  9, 10, 99,
				11,  6,  7, 10,  0,  9, 10,  2,  0, 99, 99, 99, 99,
				 2,  1, 10,  7, 11,  6,  8,  0,  3, 99, 99, 99, 99,
				 1, 10,  2,  6,  7, 11, 99, 99, 99, 99, 99, 99, 99,
				 7, 11,  6,  3,  9,  1,  3,  8,  9, 99, 99, 99, 99,
				 9,  1,  0, 11,  6,  7, 99, 99, 99, 99, 99, 99, 99,
				 0,  3,  8, 11,  6,  7, 99, 99, 99, 99, 99, 99, 99,
				11,  6,  7, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				11,  7,  6, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				 0,  8,  3, 11,  7,  6, 99, 99, 99, 99, 99, 99, 99,
				 9,  0,  1, 11,  7,  6, 99, 99, 99, 99, 99, 99, 99,
				 7,  6, 11,  3,  1,  9,  3,  9,  8, 99, 99, 99, 99,
				 1,  2, 10,  6, 11,  7, 99, 99, 99, 99, 99, 99, 99,
				 2, 10,  1,  7,  6, 11,  8,  3,  0, 99, 99, 99, 99,
				11,  7,  6, 10,  9,  0, 10,  0,  2, 99, 99, 99, 99,
				 7,  6, 11,  3,  2,  8,  8,  2, 10,  8, 10,  9, 99,
				 2,  3,  7,  2,  7,  6, 99, 99, 99, 99, 99, 99, 99,
				 8,  7,  0,  7,  6,  0,  0,  6,  2, 99, 99, 99, 99,
				 1,  9,  0,  3,  7,  6,  3,  6,  2, 99, 99, 99, 99,
				 7,  6,  2,  7,  2,  9,  2,  1,  9,  7,  9,  8, 99,
				 6, 10,  7, 10,  1,  7,  7,  1,  3, 99, 99, 99, 99,
				 6, 10,  1,  6,  1,  7,  7,  1,  0,  7,  0,  8, 99,
				 9,  0,  3,  6,  9,  3,  6, 10,  9,  6,  3,  7, 99,
				 6, 10,  7,  7, 10,  8, 10,  9,  8, 99, 99, 99, 99,
				 8,  4,  6,  8,  6, 11, 99, 99, 99, 99, 99, 99, 99,
				11,  3,  6,  3,  0,  6,  6,  0,  4, 99, 99, 99, 99,
				 0,  1,  9,  4,  6, 11,  4, 11,  8, 99, 99, 99, 99,
				 1,  9,  4, 11,  1,  4, 11,  3,  1, 11,  4,  6, 99,
				10,  1,  2, 11,  8,  4, 11,  4,  6, 99, 99, 99, 99,
				10,  1,  2, 11,  3,  6,  6,  3,  0,  6,  0,  4, 99,
				 0,  2, 10,  0, 10,  9,  4, 11,  8,  4,  6, 11, 99,
				 2, 11,  3,  6,  9,  4,  6, 10,  9, 99, 99, 99, 99,
				 3,  8,  2,  8,  4,  2,  2,  4,  6, 99, 99, 99, 99,
				 2,  0,  4,  2,  4,  6, 99, 99, 99, 99, 99, 99, 99,
				 1,  9,  0,  3,  8,  2,  2,  8,  4,  2,  4,  6, 99,
				 9,  4,  1,  1,  4,  2,  4,  6,  2, 99, 99, 99, 99,
				 8,  4,  6,  8,  6,  1,  6, 10,  1,  8,  1,  3, 99,
				 1,  0, 10, 10,  0,  6,  0,  4,  6, 99, 99, 99, 99,
				 8,  0,  3,  9,  6, 10,  9,  4,  6, 99, 99, 99, 99,
				10,  4,  6, 10,  9,  4, 99, 99, 99, 99, 99, 99, 99,
				 9,  5,  4,  7,  6, 11, 99, 99, 99, 99, 99, 99, 99,
				 4,  9,  5,  3,  0,  8, 11,  7,  6, 99, 99, 99, 99,
				 6, 11,  7,  4,  0,  1,  4,  1,  5, 99, 99, 99, 99,
				 6, 11,  7,  4,  8,  5,  5,  8,  3,  5,  3,  1, 99,
				 6, 11,  7,  1,  2, 10,  9,  5,  4, 99, 99, 99, 99,
				11,  7,  6,  8,  3,  0,  1,  2, 10,  9,  5,  4, 99,
				11,  7,  6, 10,  5,  2,  2,  5,  4,  2,  4,  0, 99,
				 7,  4,  8,  2, 11,  3, 10,  5,  6, 99, 99, 99, 99,
				 4,  9,  5,  6,  2,  3,  6,  3,  7, 99, 99, 99, 99,
				 9,  5,  4,  8,  7,  0,  0,  7,  6,  0,  6,  2, 99,
				 4,  0,  1,  4,  1,  5,  6,  3,  7,  6,  2,  3, 99,
				 7,  4,  8,  5,  2,  1,  5,  6,  2, 99, 99, 99, 99,
				 4,  9,  5,  6, 10,  7,  7, 10,  1,  7,  1,  3, 99,
				 5,  6, 10,  0,  9,  1,  8,  7,  4, 99, 99, 99, 99,
				 5,  6, 10,  7,  0,  3,  7,  4,  0, 99, 99, 99, 99,
				10,  5,  6,  4,  8,  7, 99, 99, 99, 99, 99, 99, 99,
				 5,  6,  9,  6, 11,  9,  9, 11,  8, 99, 99, 99, 99,
				 0,  9,  5,  0,  5,  3,  3,  5,  6,  3,  6, 11, 99,
				 0,  1,  5,  0,  5, 11,  5,  6, 11,  0, 11,  8, 99,
				11,  3,  6,  6,  3,  5,  3,  1,  5, 99, 99, 99, 99,
				 1,  2, 10,  5,  6,  9,  9,  6, 11,  9, 11,  8, 99,
				 1,  0,  9,  6, 10,  5, 11,  3,  2, 99, 99, 99, 99,
				 6, 10,  5,  2,  8,  0,  2, 11,  8, 99, 99, 99, 99,
				 3,  2, 11, 10,  5,  6, 99, 99, 99, 99, 99, 99, 99,
				 9,  5,  6,  3,  9,  6,  3,  8,  9,  3,  6,  2, 99,
				 5,  6,  9,  9,  6,  0,  6,  2,  0, 99, 99, 99, 99,
				 0,  3,  8,  2,  5,  6,  2,  1,  5, 99, 99, 99, 99,
				 1,  6,  2,  1,  5,  6, 99, 99, 99, 99, 99, 99, 99,
				10,  5,  6,  9,  3,  8,  9,  1,  3, 99, 99, 99, 99,
				 0,  9,  1,  5,  6, 10, 99, 99, 99, 99, 99, 99, 99,
				 8,  0,  3, 10,  5,  6, 99, 99, 99, 99, 99, 99, 99,
				10,  5,  6, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				11,  7,  5, 11,  5, 10, 99, 99, 99, 99, 99, 99, 99,
				 3,  0,  8,  7,  5, 10,  7, 10, 11, 99, 99, 99, 99,
				 9,  0,  1, 10, 11,  7, 10,  7,  5, 99, 99, 99, 99,
				 3,  1,  9,  3,  9,  8,  7, 10, 11,  7,  5, 10, 99,
				 2, 11,  1, 11,  7,  1,  1,  7,  5, 99, 99, 99, 99,
				 0,  8,  3,  2, 11,  1,  1, 11,  7,  1,  7,  5, 99,
				 9,  0,  2,  9,  2,  7,  2, 11,  7,  9,  7,  5, 99,
				11,  3,  2,  8,  5,  9,  8,  7,  5, 99, 99, 99, 99,
				10,  2,  5,  2,  3,  5,  5,  3,  7, 99, 99, 99, 99,
				 5, 10,  2,  8,  5,  2,  8,  7,  5,  8,  2,  0, 99,
				 9,  0,  1, 10,  2,  5,  5,  2,  3,  5,  3,  7, 99,
				 1, 10,  2,  5,  8,  7,  5,  9,  8, 99, 99, 99, 99,
				 1,  3,  7,  1,  7,  5, 99, 99, 99, 99, 99, 99, 99,
				 8,  7,  0,  0,  7,  1,  7,  5,  1, 99, 99, 99, 99,
				 0,  3,  9,  9,  3,  5,  3,  7,  5, 99, 99, 99, 99,
				 9,  7,  5,  9,  8,  7, 99, 99, 99, 99, 99, 99, 99,
				 4,  5,  8,  5, 10,  8,  8, 10, 11, 99, 99, 99, 99,
				 3,  0,  4,  3,  4, 10,  4,  5, 10,  3, 10, 11, 99,
				 0,  1,  9,  4,  5,  8,  8,  5, 10,  8, 10, 11, 99,
				 5,  9,  4,  1, 11,  3,  1, 10, 11, 99, 99, 99, 99,
				 8,  4,  5,  2,  8,  5,  2, 11,  8,  2,  5,  1, 99,
				 3,  2, 11,  1,  4,  5,  1,  0,  4, 99, 99, 99, 99,
				 9,  4,  5,  8,  2, 11,  8,  0,  2, 99, 99, 99, 99,
				11,  3,  2,  9,  4,  5, 99, 99, 99, 99, 99, 99, 99,
				 3,  8,  4,  3,  4,  2,  2,  4,  5,  2,  5, 10, 99,
				10,  2,  5,  5,  2,  4,  2,  0,  4, 99, 99, 99, 99,
				 0,  3,  8,  5,  9,  4, 10,  2,  1, 99, 99, 99, 99,
				 2,  1, 10,  9,  4,  5, 99, 99, 99, 99, 99, 99, 99,
				 4,  5,  8,  8,  5,  3,  5,  1,  3, 99, 99, 99, 99,
				 5,  0,  4,  5,  1,  0, 99, 99, 99, 99, 99, 99, 99,
				 3,  8,  0,  4,  5,  9, 99, 99, 99, 99, 99, 99, 99,
				 9,  4,  5, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				 7,  4, 11,  4,  9, 11, 11,  9, 10, 99, 99, 99, 99,
				 3,  0,  8,  7,  4, 11, 11,  4,  9, 11,  9, 10, 99,
				11,  7,  4,  1, 11,  4,  1, 10, 11,  1,  4,  0, 99,
				 8,  7,  4, 11,  1, 10, 11,  3,  1, 99, 99, 99, 99,
				 2, 11,  7,  2,  7,  1,  1,  7,  4,  1,  4,  9, 99,
				 3,  2, 11,  4,  8,  7,  9,  1,  0, 99, 99, 99, 99,
				 7,  4, 11, 11,  4,  2,  4,  0,  2, 99, 99, 99, 99,
				 2, 11,  3,  7,  4,  8, 99, 99, 99, 99, 99, 99, 99,
				 2,  3,  7,  2,  7,  9,  7,  4,  9,  2,  9, 10, 99,
				 4,  8,  7,  0, 10,  2,  0,  9, 10, 99, 99, 99, 99,
				 2,  1, 10,  0,  7,  4,  0,  3,  7, 99, 99, 99, 99,
				10,  2,  1,  8,  7,  4, 99, 99, 99, 99, 99, 99, 99,
				 9,  1,  4,  4,  1,  7,  1,  3,  7, 99, 99, 99, 99,
				 1,  0,  9,  8,  7,  4, 99, 99, 99, 99, 99, 99, 99,
				 3,  4,  0,  3,  7,  4, 99, 99, 99, 99, 99, 99, 99,
				 8,  7,  4, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				 8,  9, 10,  8, 10, 11, 99, 99, 99, 99, 99, 99, 99,
				 0,  9,  3,  3,  9, 11,  9, 10, 11, 99, 99, 99, 99,
				 1, 10,  0,  0, 10,  8, 10, 11,  8, 99, 99, 99, 99,
				10,  3,  1, 10, 11,  3, 99, 99, 99, 99, 99, 99, 99,
				 2, 11,  1,  1, 11,  9, 11,  8,  9, 99, 99, 99, 99,
				11,  3,  2,  0,  9,  1, 99, 99, 99, 99, 99, 99, 99,
				11,  0,  2, 11,  8,  0, 99, 99, 99, 99, 99, 99, 99,
				11,  3,  2, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				 3,  8,  2,  2,  8, 10,  8,  9, 10, 99, 99, 99, 99,
				 9,  2,  0,  9, 10,  2, 99, 99, 99, 99, 99, 99, 99,
				 8,  0,  3,  1, 10,  2, 99, 99, 99, 99, 99, 99, 99,
				10,  2,  1, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				 8,  1,  3,  8,  9,  1, 99, 99, 99, 99, 99, 99, 99,
				 9,  1,  0, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				 8,  0,  3, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
				99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99};


			// source: https://github.com/SebLague/Marching-Cubes
			//
			//byte[] cornerIndexAFromEdge = { 0, 1, 2, 3, 4, 5, 6, 7, 0, 1, 2, 3 };

			//byte[] cornerIndexBFromEdge = { 1, 2, 3, 0, 5, 6, 7, 4, 4, 5, 6, 7 };

			byte[] cornerIndexMixed = { 0, 1, 2, 3, 4, 5, 6, 7, 0, 1, 2, 3, 1, 2, 3, 0, 5, 6, 7, 4, 4, 5, 6, 7 };



			// Precaulculate indices:
			// Vertices are not shared, so out index array looks like (0,1,2, 3,4,5, 6,7,8...)
			// We can use precalculated array, but must be big enough (PrecalculatedIndicesCount)
			//
			indicesPrecalc32bit = new NativeArray<int>(PrecalculatedIndicesCount, Allocator.Persistent);
			for (int i = 0; i < PrecalculatedIndicesCount; i++)
				indicesPrecalc32bit[i] = i;
			
			// Additionaly, we can use 16 bit indices when output vertex list is small enough
			//
			indicesPrecalc16bit = new NativeArray<ushort>(ushort.MaxValue + 1, Allocator.Persistent);
			for (int i = 0; i <= ushort.MaxValue; i++)
				indicesPrecalc16bit[i] = (ushort)i;



			// Shuffle triangulation subtables
			// Triangulation table is used to obtain proper indices from array of 8 voxel samples. Each sampel is a cube corner.
			// We use cornermask (8 bit mask) as index to triangulation table. Those bits in masks correspond to voxel cube (2x2x2)
			// But we use different ordering for sampled voxels to make SIMD stuff in MeshingJob easier.
			// 

			byte[] selectedTriangulationArray;
			if (TriangulationSubTableLenght == 13) selectedTriangulationArray = triangulationArrayAlternative;
			else if (TriangulationSubTableLenght == 16) selectedTriangulationArray = triangulationArray;

			triangulationTable = new NativeArray<byte>(selectedTriangulationArray.Length, Allocator.Persistent);

			for (int i = 0; i < 256; i++)
			{
				var j = ShuffleTriangulationIndexBits(i);
				for (int k = 0; k < TriangulationSubTableLenght; k++)
				{
					triangulationTable[j * TriangulationSubTableLenght + k] = selectedTriangulationArray[i * TriangulationSubTableLenght + k];
				}
			}

			cornerIndexMix = new NativeArray<byte>(cornerIndexMixed, Allocator.Persistent);
			cornerIndexA = cornerIndexMix.GetSubArray(0, 12); // new NativeArray<byte>(cornerIndexAFromEdge, Allocator.Persistent);
			cornerIndexB = cornerIndexMix.GetSubArray(12, 12); // new NativeArray<byte>(cornerIndexBFromEdge, Allocator.Persistent);
		}




		/// <summary>
		/// Get proper index for triangulation subtable.
		/// </summary>
		private static int ShuffleTriangulationIndexBits(int i)
		{
			// default cornermask :	76 54 32 10
			// new order :			67 23 54 10
			//
			// 0 => 0
			// 1 => 1
			// 2 => 5
			// 3 => 4
			// 4 => 2
			// 5 => 3
			// 6 => 7
			// 7 => 6

			int ret = 0;
			if ((i & 1 << 0) != 0) ret |= 1 << 0;
			if ((i & 1 << 1) != 0) ret |= 1 << 1;
			if ((i & 1 << 2) != 0) ret |= 1 << 5;
			if ((i & 1 << 3) != 0) ret |= 1 << 4;
			if ((i & 1 << 4) != 0) ret |= 1 << 2;
			if ((i & 1 << 5) != 0) ret |= 1 << 3;
			if ((i & 1 << 6) != 0) ret |= 1 << 7;
			if ((i & 1 << 7) != 0) ret |= 1 << 6;
			return ret;
		}

		private struct Nibble
		{
			public byte data;

			public Nibble(byte low, byte high)
			{
				data = (byte)(low + (high << 4));
			}
			public int low => data & 0x0F;
			public int high => (data & 0xF0) >> 4;
		}
	}
}