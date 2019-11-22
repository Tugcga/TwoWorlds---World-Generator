using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace WorldGenerator
{
    public class WG_SegmentsGenerator
    {
        public List<WG_LocationController> GenerateLocationSegments(WG_TerrainBuilder builder, float segmentSize, float meshSquareSize, int meshSquaresCount, int segmentMinX, int segmentMaxX, int segmentMinY, int segmenMaxY, GameObject rootObject, Material floorMaterial, Material heightMaterial, Material wallsMaterial, bool[,] map, float height, bool bakeNavMesh, NavMeshModifierVolume navMeshCutter, float uvPadding)
        {
            List<WG_LocationController> locations = new List<WG_LocationController>();
            for (int u = segmentMinX; u < segmentMaxX + 1; u++)
            {
                for (int v = segmentMinY; v < segmenMaxY + 1; v++)
                {
                    bool[,] segmentMap = new bool[meshSquaresCount + 1, meshSquaresCount + 1];
                    //fill data for one segment
                    for (int x = 0; x < meshSquaresCount + 1; x++)
                    {
                        for (int y = 0; y < meshSquaresCount + 1; y++)
                        {
                            segmentMap[x, y] = map[(u - segmentMinX) * (meshSquaresCount) + x, (v - segmentMinY) * (meshSquaresCount) + y];
                        }
                    }
                    locations.Add(EmitSegment(u, v, segmentSize, meshSquareSize, rootObject.transform, segmentMap, floorMaterial, wallsMaterial, height, uvPadding));
                }
            }

            navMeshCutter.center = new Vector3((segmentMinX + segmentMaxX) * segmentSize / 2.0f, height, (segmentMinY + segmenMaxY) * segmentSize / 2.0f);
            navMeshCutter.size = new Vector3((segmentMaxX - segmentMinX + 1) * segmentSize, 1, (segmenMaxY - segmentMinY + 1) * segmentSize);
            BuilNavMesh(builder, locations, bakeNavMesh);

            return locations;
        }

        WG_LocationController EmitSegment(int u, int v, float segmentSize, float meshSquareSize, Transform root, bool[,] map, Material floorMaterial, Material wallsMaterial, float height, float uvPadding)
        {
            //create game object
            GameObject location = new GameObject() { name = "location_" + u.ToString() + "_" + v.ToString()};
            location.transform.SetParent(root);
            location.transform.position = new Vector3(u * segmentSize, 0.0f, v * segmentSize);
            WG_MarchingSquares marchingSquares = new WG_MarchingSquares();
            MeshDataClass mountainsData = marchingSquares.GenerateMesh(map, false, -segmentSize / 2, segmentSize / 2, -segmentSize / 2, segmentSize / 2, meshSquareSize, height, false, false, 0f);
            MeshDataClass floorData = marchingSquares.GenerateMesh(map, true, -segmentSize / 2, segmentSize / 2, -segmentSize / 2, segmentSize / 2, meshSquareSize, 0, true, true, height);
            //get walls vertices and triangles from generaton of the floor
            MeshDataClass wallsData = marchingSquares.GetWallsData();
            
            //union ground and mountains
            Vector3[] vertices = new Vector3[mountainsData.vertices.Count + floorData.vertices.Count];
            int mountainsVerticesCount = mountainsData.vertices.Count;
            for (int i = 0; i < mountainsVerticesCount; i++)
            {
                vertices[i] = mountainsData.vertices[i];
            }

            for (int i = 0; i < floorData.vertices.Count; i++)
            {
                vertices[mountainsVerticesCount + i] = floorData.vertices[i];
            }
            //re-enumerate triangles
            int[] triangles = new int[mountainsData.triangles.Count + floorData.triangles.Count];
            int mountainsTrianglesCount = mountainsData.triangles.Count;
            for (int i = 0; i < mountainsTrianglesCount; i++)
            {
                triangles[i] = mountainsData.triangles[i];
            }

            for (int i = 0; i < floorData.triangles.Count; i++)
            {
                triangles[mountainsTrianglesCount + i] = floorData.triangles[i] + mountainsVerticesCount;
            }
            GameObject groundGO = new GameObject("Ground") {isStatic = true};
            groundGO.transform.SetParent(location.transform, false);
            MeshRenderer groundRenderer = groundGO.AddComponent<MeshRenderer>();
            groundRenderer.material = floorMaterial;
            Mesh groundMesh = new Mesh() { vertices = vertices, triangles = triangles };

            groundMesh.Simplify();
            groundMesh.RecalculateNormals();
            //ground uv
            Vector2[] groundUV = new Vector2[groundMesh.vertexCount];
            Vector2[] groundUV2 = new Vector2[groundMesh.vertexCount];
            Vector3[] groundVertices = groundMesh.vertices;
            Vector2[] vertexNormals = marchingSquares.GetVertexNormals(groundMesh.vertices, groundMesh.triangles);
            for (int i = 0; i < groundVertices.Length; i++)
            {
                groundUV[i] = new Vector2(0.5f + groundVertices[i].x / segmentSize, 0.5f + groundVertices[i].z / segmentSize);
                groundUV2[i] = groundUV[i] - uvPadding * vertexNormals[i];
            }

            groundMesh.uv = groundUV;
            groundMesh.uv2 = groundUV2;

            MeshFilter groundFilter = groundGO.AddComponent<MeshFilter>();
            groundFilter.mesh = groundMesh;

            //next walls
            bool createWalls = false;
            GameObject wallsGO = new GameObject("Walls") { isStatic = true };
            if (wallsData.vertices.Count > 0)
            {
                createWalls = true;
                wallsGO.transform.SetParent(location.transform, false);
                MeshRenderer wallsRenderer = wallsGO.AddComponent<MeshRenderer>();
                wallsRenderer.material = wallsMaterial;
                Mesh wallsMesh = new Mesh() { vertices = wallsData.vertices.ToArray(), triangles = wallsData.triangles.ToArray() };

                wallsMesh.RecalculateNormals();
                //next we should calculate uv coordinates
                //all polygons are separate 4-sided polygons
                Vector2[] wallsUV = marchingSquares.GetWallsUV();
                Vector2[] uv1 = new Vector2[wallsUV.Length];
                Vector2[] uv2 = new Vector2[wallsUV.Length];
                //uv2 should be fit to the square [0, 1]x[0, 1]
                //for uv1 rescale 1 to walls height
                float wallLength = marchingSquares.GetWallLength();
                for (int i = 0; i < wallsUV.Length; i++)
                {
                    Vector2 uv = wallsUV[i];
                    uv1[i] = new Vector2(uv.x / height, uv.y);
                    uv2[i] = new Vector2(uv.x / wallLength, uv.y);
                }

                wallsMesh.uv = uv1;
                wallsMesh.uv2 = uv2;

                MeshFilter wallsFilter = wallsGO.AddComponent<MeshFilter>();
                wallsFilter.mesh = wallsMesh;
            }
            

            WG_LocationController locationController = location.AddComponent<WG_LocationController>();
            locationController.groundGO = groundGO;
            locationController.wallsGO = createWalls ? wallsGO : null;
            locationController.u = u;
            locationController.v = v;

            if (!createWalls)
            {
                UnityEngine.Object.DestroyImmediate(wallsGO);
            }

            return locationController;
        }

        void BuilNavMesh(WG_TerrainBuilder builder, List<WG_LocationController> locations, bool bakeNavMesh)
        {
            for (int i = 0; i < locations.Count; i++)
            {
                GameObject ground = locations[i].groundGO;
                NavMeshSurface surface = ground.AddComponent<NavMeshSurface>();
            }

            if (bakeNavMesh)
            {
                //call bake from the first location
                if (locations.Count > 0)
                {
                    NavMeshSurface navSurface = locations[0].groundGO.GetComponent<NavMeshSurface>();
                    NavMeshBuildSettings settings = navSurface.GetBuildSettings();

                    builder.SaveNavMeshData(navSurface.navMeshData);
                }
            }
        }
    }
}
