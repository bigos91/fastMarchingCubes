# fastMarchingCubes
- Fast implementation of Marching Cubes in Unity, using Burst and SIMD instructions.
- Less than 1ms (around ~0.2-0.5 depends on complexity of meshed area)
- Similiar to : https://github.com/bigos91/fastNaiveSurfaceNets
- Triangulation table is a bit different than original - different corner indexing is used to make simd stuff possible
![alt text](https://github.com/bigos91/fastMarchingCubes/blob/main/screen.jpg?raw=true)
https://www.youtube.com/watch?v=fIzZdO7FxqQ&ab_channel=Bigos91

#### Features:
- 2 different triangulation tables (read Mesher.Arrays.cs)
- Naive version, SIMD, and SIMD multithreaded.
- No normals generated, intead they are calculated in fragment shader (ddx,ddy)
- No comment on simd stuff - if you need explanation read https://github.com/bigos91/fastNaiveSurfaceNets it is same.
- Cornermask calculations are done using SIMD stuff, 32 cubes at time (32x2x2 voxels), reusing values calculated from previous loop steps.

#### Problems:
- Multithreaded version sometimes gives better performance results, sometimes not. No idea why.

#### Limitations:
- Meshed area must have 32 voxels in at least one dimension. (SIMD implementation support only chunks 32^3, but it is possible to make it working with 32xNxM)

#### Requirements:
- Unity (2020.3 works fine, dont know about previous versions)
- CPU with SSE4.1 support (around year 2007)

#### Usage:
- Clone, run, open scene [FastMarchingCubes/Scenes/SampleScene],
- Disable everything what makes burst safe to make it faster :)

#### Resources:
- http://paulbourke.net/geometry/polygonise/ - table
- http://paulbourke.net/geometry/polygonise/table2.txt - alternative table
- https://github.com/SebLague/Marching-Cubes - corner tables
- https://github.com/Chaser324/unity-wireframe - for wireframe.

#### Todo:
 - 16^3 size version
 - maybe 64^3 size version but on AVX
