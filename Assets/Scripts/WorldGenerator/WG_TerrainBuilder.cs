using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEngine.SceneManagement;
#endif

namespace WorldGenerator
{
    [ExecuteInEditMode]
    public class WG_TerrainBuilder : MonoBehaviour
    {
        public bool visualizeMap;
        public bool visualizeBoundaryEdges;
        public float visualAreaSize;
        //public Vector2 visualAreaCenter;
        [Range(0, 1)]
        public float gizmoAlpha = 0.5f;
        public Transform visualAreaCenter;
        public Color visualZoneLine = Color.cyan;
        public Color boundaryLine = Color.red;
        int visualItemsCount;
        private bool[,] visualMap;  // true - mountains, false - walkable area
        private float visualSquareSize;

        [Range(0, 10)]
        public float mountainsLimit;

        public int meshSquaresCount; // the count of squares in the mesh
        public float segmentSize; // the size of one location segment
        public int segmentsMinX;  // indexes of segments in generated area
        public int segmenstMaxX;
        public int segmentsMinY;
        public int segmenstMaxY;

        public float zoneWidth;
        public float zoneHeight;

        public float mountainsHeight;
        [Range(0, 0.5f)]
        public float uv2Padding = 0.02f;
        public string meshRootName;
        public Material groundMaterial;
        public Material mountainsMaterial;
        public Material wallsMaterial;

        public bool clearAll = false;
        public string groundName = "Ground";
        public string wallsName = "Walls";

        public NavMeshModifierVolume navMeshCutVolume;

        public bool forceUpdate;
        public bool buildMesh;

        private NavMeshData navmeshData;
        private bool navmeshExist;
        

        void Update()
        {
            if (forceUpdate)
            {
                UpdateMap();
                if (buildMesh)
                {
                    BuildMesh();
                }
            }
        }

        void OnValidate()
        {
            
        }

        bool IsPointMountains(Vector2 point, WG_Primitive_BaseNoise[] noises)
        {
            if (point.x > zoneWidth / 2 || point.x < -zoneWidth / 2 || point.y > zoneHeight / 2 || point.y < -zoneHeight / 2)
            {
                return true;
            }

            bool toReturn = false;
            float maxLevel = float.MinValue;
            for (int noiseIndex = 0; noiseIndex < noises.Length; noiseIndex++)
            {
                if (noises[noiseIndex].isActive)
                {
                    FloatBool height = noises[noiseIndex].GetHeight(point);
                    additiveEnum mode = noises[noiseIndex].additive;
                    float level = noises[noiseIndex].importantLevel;
                    if (height.boolVal && height.floatVal > mountainsLimit && level > maxLevel)
                    {
                        maxLevel = level;
                        if (mode == additiveEnum.Additive)
                        {
                            toReturn = true;
                        }
                        else
                        {
                            toReturn = false;
                        }
                    }
                }
            }

            return toReturn;
        }

        public void BuildMesh()
        {
#if UNITY_EDITOR
            //delete all combined objects
            WG_Tag_CombineMesh[] combs = FindObjectsOfType<WG_Tag_CombineMesh>();
            for(int i = 0; i < combs.Length; i++)
            {
                DestroyImmediate(combs[i].gameObject);
            }
            
            //find all objects with tower or position tag
            WG_PositionBase[] objs = WG_Helper.FindObjectsOfTypeAll<WG_PositionBase>().ToArray();
            for(int i = 0; i < objs.Length; i++)
            {
                WG_Tag_MasterPrefab tagMaster = objs[i].gameObject.GetComponent<WG_Tag_MasterPrefab>();
                if(tagMaster != null)
                {
                    objs[i].gameObject.SetActive(true);
                    DestroyImmediate(tagMaster);
                }

                Transform tfm = objs[i].transform;
                IntPair loc = WG_Helper.GetLocationCoordinates(tfm.position, segmentSize);
                if(loc.u >= segmentsMinX && loc.u <= segmenstMaxX && loc.v >= segmentsMinY && loc.v <= segmenstMaxY)
                {
                    //parent to the generator root
                    tfm.SetParent(gameObject.transform, true);
                }
                else
                {//unparent object if it outside of the generated zone
                    tfm.SetParent(null, true);
                }
            }

            //clear generated segments
            navmeshExist = false;
            //create helper object for all sub-generated objects
            GameObject store = new GameObject() {name = "Temp Store"};
            Transform[] allTransforms = gameObject.GetComponentsInChildren<Transform>();
            for (int trIndex = allTransforms.Length - 1; trIndex >= 0; trIndex--)
            {
                GameObject go = allTransforms[trIndex].gameObject;
                //try to get noise primitive component
                //if this component is null, then delete game object
                WG_Primitive_PerlinNoise noiseComponent = go.GetComponent<WG_Primitive_PerlinNoise>();
                WG_Primitive_Paint paintComponent = go.GetComponent<WG_Primitive_Paint>();
                WG_Painter painterComponent = go.GetComponent<WG_Painter>();
                WG_VisualCenter visCenterComponent = go.GetComponent<WG_VisualCenter>();
                if (go != gameObject && noiseComponent == null && paintComponent == null && painterComponent == null && visCenterComponent == null)
                {
                    if (clearAll)
                    {
                        DestroyImmediate(go);
                    }
                    else
                    {
                        //check is this object contains location controller
                        WG_LocationController locController = go.GetComponent<WG_LocationController>();
                        if (locController != null)
                        {//this is location root object, desrtoy it
                            DestroyImmediate(go);
                        }
                        else
                        {//this is not location root, may be walls or ground object
                            if (go.name == groundName || go.name == wallsName || go.name == meshRootName)
                            {//this is generated ground or wall object, destoy it
                                DestroyImmediate(go);
                            }
                            else
                            {//this is sub-placed object, store it
                                if (PrefabUtility.GetOutermostPrefabInstanceRoot(go) == null || PrefabUtility.GetOutermostPrefabInstanceRoot(go) == go)
                                {
                                    go.transform.SetParent(store.transform, true);
                                }
                            }
                        }
                    }
                }
            }
            //fill map for all location segments
            bool[,] map = new bool[(segmenstMaxX - segmentsMinX + 1) * meshSquaresCount + 1, (segmenstMaxY - segmentsMinY + 1) * meshSquaresCount + 1];
            WG_Primitive_BaseNoise[] noises = gameObject.GetComponentsInChildren<WG_Primitive_BaseNoise>();
            float meshSquareSize = segmentSize / meshSquaresCount;
            Vector2 bottomLeftCorner = new Vector2((segmentsMinX - 0.5f) * segmentSize, (segmentsMinY - 0.5f) * segmentSize);
            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    Vector2 pos = new Vector2(x * meshSquareSize, y * meshSquareSize) + bottomLeftCorner;
                    map[x, y] = IsPointMountains(pos, noises);
                }
            }
            
            //next create root for location segments
            GameObject locationsRoot = new GameObject {name = meshRootName};
            locationsRoot.transform.SetParent(gameObject.transform);
            //create location generator
            WG_SegmentsGenerator generator = new WG_SegmentsGenerator();
            List<WG_LocationController> locations = generator.GenerateLocationSegments(this, segmentSize, meshSquareSize, meshSquaresCount, segmentsMinX, segmenstMaxX, segmentsMinY, segmenstMaxY, locationsRoot, groundMaterial, mountainsMaterial, wallsMaterial, map, mountainsHeight, !(forceUpdate && buildMesh), navMeshCutVolume, uv2Padding);
#endif

#if UNITY_EDITOR
            //after generation we should place back all stored objects, we will place it inside location root object
            Transform[] storedObjects = store.GetComponentsInChildren<Transform>();
            for (int tfmIndex = 0; tfmIndex < storedObjects.Length; tfmIndex++)
            {
                Transform tfm = storedObjects[tfmIndex];
                if (tfm.gameObject != store)
                {
                    Vector3 position = tfm.position;
                    IntPair locCoords = WG_Helper.GetLocationCoordinates(position, segmentSize);
                    WG_LocationController loc = GetLocationByCoordinates(locCoords, locations);
                    if (loc != null)
                    {
                        //rever prefab properties (mesh and material)
                        GameObject prefabMaster = PrefabUtility.GetOutermostPrefabInstanceRoot(tfm.gameObject);
                        MeshFilter meshComponent = tfm.GetComponent<MeshFilter>();
                        if ((prefabMaster == null && meshComponent != null) || prefabMaster == tfm.gameObject)
                        {
                            Vector3 scale = tfm.localScale;
                            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(tfm.gameObject);
                            if (source != null)
                            {
                                PrefabUtility.RevertPrefabInstance(tfm.gameObject, InteractionMode.UserAction);
                            }
                            
                            tfm.localScale = scale;

                            //move object inside location
                            tfm.SetParent(loc.gameObject.transform, true);
                            tfm.position = position;
                        }
                    }
                    else
                    {
                        Debug.Log("There is no locations with coordinates " + locCoords.ToString() + ", so skip the object " + tfm.gameObject.name);
                    }
                }
            }
            //delete temp store
            DestroyImmediate(store);
            #endif

            BuildNavmeshOnly();
        }

        WG_LocationController GetLocationByCoordinates(IntPair coords, List<WG_LocationController> locations)
        {
            for (int i = 0; i < locations.Count; i++)
            {
                WG_LocationController loc = locations[i];
                if (loc.u == coords.u && loc.v == coords.v)
                {
                    return loc;
                }
            }
            return null;
        }

        public void UpdateMap()
        {
            visualItemsCount = (int)(visualAreaSize / (segmentSize / meshSquaresCount));
            visualMap = new bool[visualItemsCount,visualItemsCount];
            //fill visualMap
            //find all noise primitives
            WG_Primitive_BaseNoise[] noises = gameObject.GetComponentsInChildren<WG_Primitive_BaseNoise>();
            for (int x = 0; x < visualItemsCount; x++)
            {
                for (int y = 0; y < visualItemsCount; y++)
                {
                    Vector2 pos = new Vector2(-visualAreaSize / 2 + (x + 0.5f) * visualSquareSize + visualAreaCenter.position.x, -visualAreaSize / 2 + (y + 0.5f) * visualSquareSize + visualAreaCenter.position.z);
                    visualMap[x, y] = IsPointMountains(pos, noises);
                }
            }
            visualSquareSize = visualAreaSize / visualItemsCount;
        }

        public void BuildNavmeshOnly()
        {
            WG_LocationController[] locations = GetComponentsInChildren<WG_LocationController>();
            if (locations.Length > 0)
            {
                WG_LocationController loc = locations[0];
                NavMeshSurface navSurface = loc.groundGO.GetComponent<NavMeshSurface>();
                navSurface.BuildNavMesh();

                SaveNavMeshData(navSurface.navMeshData);
            }
        }

        public void SaveNavMeshData(NavMeshData _data)
        {
            navmeshData = _data;
            navmeshExist = true;
        }

        public NavMeshData GetNavmeshData()
        {
            if (navmeshExist)
            {
                return navmeshData;
            }
            else
            {
                Debug.Log("Navmesh does not generated.");
                return null;
            }
        }

        Vector3 GetPositionOfSegmetnCorner(int x, int y)
        {
            return new Vector3(x * segmentSize / 2, 0, y * segmentSize / 2);
        }

        void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            if (visualMap != null && visualizeMap)
            {
                for (int x = 0; x < visualMap.GetLength(0); x++)
                {
                    for (int y = 0; y < visualMap.GetLength(1); y++)
                    {
                        Gizmos.color = (visualMap[x, y]) ? new Color(0, 0, 0, gizmoAlpha) : new Color(1, 1, 1, gizmoAlpha);
                        Vector3 pos = new Vector3(-visualAreaSize / 2 + (x + 0.5f) * visualSquareSize + visualAreaCenter.position.x, 0, -visualAreaSize / 2 + (y + 0.5f) * visualSquareSize + visualAreaCenter.position.z);
                        Gizmos.DrawCube(pos, new Vector3(visualSquareSize * 0.75f, 0.0f, visualSquareSize * 0.75f));
                    }
                }

                Vector2 minCorner = new Vector2(visualAreaCenter.position.x - visualAreaSize / 2, visualAreaCenter.position.z - visualAreaSize / 2);
                Vector2 maxCorner = new Vector2(visualAreaCenter.position.x + visualAreaSize / 2, visualAreaCenter.position.z + visualAreaSize / 2);
                
                int minX = (int)(minCorner.x / (segmentSize / 2)) + (minCorner.x > 0 ? 1 : 0);
                int maxX = (int)(maxCorner.x / (segmentSize / 2)) + (maxCorner.x > 0 ? 1 : 0);

                int minY = (int)(minCorner.y / (segmentSize / 2)) + (minCorner.y > 0 ? 1 : 0);
                int maxY = (int)(maxCorner.y / (segmentSize / 2)) + (maxCorner.y > 0 ? 1 : 0);

                Gizmos.color = visualZoneLine;
                for (int x = minX - 1; x < maxX + 1; x++)
                {
                    for (int y = minY - 1; y < maxY + 1; y++)
                    {
                        if (x % 2 == 0 && y % 2 == 0)
                        {
                            Gizmos.DrawLine(GetPositionOfSegmetnCorner(x - 1, y - 1), GetPositionOfSegmetnCorner(x + 1, y - 1));
                            Gizmos.DrawLine(GetPositionOfSegmetnCorner(x + 1, y - 1), GetPositionOfSegmetnCorner(x + 1, y + 1));
                            Gizmos.DrawLine(GetPositionOfSegmetnCorner(x + 1, y + 1), GetPositionOfSegmetnCorner(x - 1, y + 1));
                            Gizmos.DrawLine(GetPositionOfSegmetnCorner(x - 1, y + 1), GetPositionOfSegmetnCorner(x - 1, y - 1));
                            
                            string labelString = "(" + (x / 2).ToString() + ", " + (y / 2).ToString() + ")";
                            Vector3 labelPos = new Vector3(x * segmentSize / 2, 2.0f, y * segmentSize / 2);
                            Handles.Label(labelPos, labelString);
                        }
                    }
                }
            }

            if(visualizeBoundaryEdges)
            {
                List<BoundaryEdge> boundary = WG_Helper.GetNavmeshBoundary();

                Gizmos.color = boundaryLine;
                for (int i = 0; i < boundary.Count; i++)
                {
                    BoundaryEdge edge = boundary[i];
                    if (true)
                    {
                        Gizmos.DrawLine(edge.start, edge.end);
                        //Handles.DrawSolidDisc(edge.start, Vector3.up, 0.03f);
                        //Handles.Label(edge.start, edge.startVertex.ToString());
                        //Handles.DrawSolidDisc(edge.end, Vector3.up, 0.03f);
                        //Handles.Label(edge.end, edge.endVertes.ToString());
                        //Handles.Label((edge.start + edge.end) / 2.0f, edge.index.ToString());
                    }
                }
            }
            #endif
        }

        void CombineLocation(GameObject locationRoot)
        {
#if UNITY_EDITOR
            //we should collect objects with the same material
            Dictionary<int, List<CombinerDataContainer>> materialMap = new Dictionary<int, List<CombinerDataContainer>>();
            Dictionary<int, Material> materials = new Dictionary<int, Material>();

            Transform[] allTransforms = locationRoot.transform.GetComponentsInChildren<Transform>();
            for(int i = 0; i < allTransforms.Length; i++)
            {
                GameObject go = allTransforms[i].gameObject;
                WG_LocationController locController = go.GetComponent<WG_LocationController>();
                if(locController == null)
                {
                    //Debug.Log(allTransforms[i].gameObject.name);
                    GameObject prefab = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                    if(prefab != null && prefab == go)  // ignore individual generated objects
                    {//this object is prefab, so, dive to it subobjects
                        //use this code only when we consider the root prefab object and ignore for all it subobjects
                        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
                        Transform[] sourceSubtfms = source.transform.GetComponentsInChildren<Transform>();
                        Transform goTfm = go.transform;
                        if (sourceSubtfms.Length == 1)
                        {
                            //prefab contains only one object (it builded by proBuilder)
                            //use changed mesh filter of the original scene object
                            MeshFilter mFilter = go.GetComponent<MeshFilter>();
                            MeshRenderer mRenderer = go.GetComponent<MeshRenderer>();
                            if(mFilter != null && mRenderer != null)
                            {
                                Material m = mRenderer.sharedMaterial;
                                int mId = m.GetInstanceID();
                                if (materialMap.ContainsKey(mId))
                                {
                                    materialMap[mId].Add(new CombinerDataContainer() { go = go, worldMatrix = go.transform.localToWorldMatrix /*toCenterTfm = goTfm*/ });
                                }
                                else
                                {
                                    materialMap.Add(mId, new List<CombinerDataContainer>() { new CombinerDataContainer() { go = go, worldMatrix = go.transform.localToWorldMatrix /*toCenterTfm = goTfm */} });
                                    materials.Add(mId, m);
                                }
                            }
                        }
                        else
                        {//for large prefab we use master prefab objects
                            for (int j = 0; j < sourceSubtfms.Length; j++)
                            {
                                GameObject subObj = sourceSubtfms[j].gameObject;
                                MeshFilter mFilter = subObj.GetComponent<MeshFilter>();
                                MeshRenderer mRenderer = subObj.GetComponent<MeshRenderer>();
                                if (mFilter != null && mRenderer != null && mFilter.sharedMesh != null)  // ignore subobject, if the master prefab does not contains the mesh
                                {//this object contains mesh
                                    Material m = mRenderer.sharedMaterial;
                                    int mId = m.GetInstanceID();
                                    if (materialMap.ContainsKey(mId))
                                    {
                                        materialMap[mId].Add(new CombinerDataContainer() { go = subObj, worldMatrix = go.transform.localToWorldMatrix * subObj.transform.localToWorldMatrix /*toCenterTfm = goTfm, localTfm = subObj.transform*/});
                                    }
                                    else
                                    {
                                        materialMap.Add(mId, new List<CombinerDataContainer>() { new CombinerDataContainer() { go = subObj, worldMatrix = go.transform.localToWorldMatrix * subObj.transform.localToWorldMatrix /*toCenterTfm = goTfm, localTfm = subObj.transform*/ } });
                                        materials.Add(mId, m);
                                    }
                                }
                            }
                        }

                        //mark original objects and disable it
                        go.AddComponent<WG_Tag_MasterPrefab>();
                        go.SetActive(false);
                    }
                }
            }

            //builded material map
            Matrix4x4 rootMatrix = locationRoot.transform.worldToLocalMatrix;
            foreach (int mId in materialMap.Keys)
            {
                //Debug.Log(mId.ToString() + ": " + materials[mId].name);
                //we should create new mesh for all objects with this material
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                List<Vector2> uvs = new List<Vector2>();
                List<Vector3> normals = new List<Vector3>();
                List<Vector4> tangents = new List<Vector4>();

                int indexShift = 0;

                for(int i = 0; i < materialMap[mId].Count; i++)
                {
                    GameObject go = materialMap[mId][i].go;  // here we should use transform relative to the master prefab (we should store it separatly)
                    //Debug.Log("\tobject " + go.name);
                    //Transform tfm = materialMap[mId][i].toCenterTfm;
                    //Matrix4x4 globalMatrix = tfm.localToWorldMatrix;

                    //Matrix4x4   = rootMatrix * globalMatrix;
                    Matrix4x4 changeMatrix = rootMatrix * materialMap[mId][i].worldMatrix;
                    //if(materialMap[mId][i].localTfm != null)
                    //{
                    //changeMatrix = changeMatrix * materialMap[mId][i].localTfm.localToWorldMatrix;
                    //}

                    MeshFilter mFilter = go.GetComponent<MeshFilter>();
                    Mesh mesh = mFilter.sharedMesh;
                    Vector3[] vs = mesh.vertices;
                    for (int j = 0; j < vs.Length; j++)
                    {
                        vertices.Add(changeMatrix.MultiplyPoint(vs[j]));
                    }
                    //Debug.Log("\t\tvetices: " + vs.Length.ToString());
                    int[] trs = mesh.triangles;
                    for (int j = 0; j < trs.Length; j++)
                    {
                        triangles.Add(trs[j] + indexShift);
                    }

                    Vector2[] mvs = mesh.uv;
                    for(int j = 0; j < vs.Length; j++)
                    {
                        uvs.Add(mvs[j]);
                    }

                    Vector3[] nms = mesh.normals;
                    for(int j = 0; j < nms.Length; j++)
                    {
                        normals.Add(changeMatrix.MultiplyVector(nms[j]).normalized);
                    }
                    Vector4[] tgs = mesh.tangents;
                    for(int j = 0; j < tgs.Length; j++)
                    {
                        Vector4 t = changeMatrix.MultiplyVector(tgs[j]);
                        t.w = tgs[j].w;
                        tangents.Add(t);
                    }
                    //Debug.Log("\t\tnormals: " + nms.Length.ToString());

                    indexShift += vs.Length;
                    
                }

                Mesh combinedMesh = new Mesh();
                combinedMesh.vertices = vertices.ToArray();
                combinedMesh.triangles = triangles.ToArray();
                combinedMesh.uv = uvs.ToArray();
                combinedMesh.normals = normals.ToArray();
                combinedMesh.tangents = tangents.ToArray();
                //Debug.Log("\tset vertices: " + vertices.Count.ToString() + ", normals: " + normals.Count.ToString());

                Unwrapping.GenerateSecondaryUVSet(combinedMesh);

                //create game object
                GameObject combinedGO = new GameObject("combined_" + materials[mId].name) { isStatic = true };
                MeshFilter cmf = combinedGO.AddComponent<MeshFilter>();
                cmf.mesh = combinedMesh;

                combinedGO.transform.SetParent(locationRoot.transform, false);
                MeshRenderer cmr = combinedGO.AddComponent<MeshRenderer>();
                cmr.material = materials[mId];

                combinedGO.AddComponent<WG_Tag_CombineMesh>();
                //StaticEditorFlags flags = StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI;
                //GameObjectUtility.SetStaticEditorFlags(combinedGO, flags);
            }
#endif
        }

        public void CombineCommand()
        {
#if UNITY_EDITOR
            /*for(int i = 0; i < gameObject.transform.childCount; i++)
            {
                GameObject go = transform.GetChild(i).gameObject;
                if(go.name == "Locations")
                {
                    //this is root location object
                    for(int l = 0; l < go.transform.childCount; l++)
                    {
                        GameObject locGO = go.transform.GetChild(l).gameObject;
                        //next we should process this game object
                        CombineLocation(locGO);
                    }
                }
            }*/

            WG_LocationController[] locs = gameObject.GetComponentsInChildren<WG_LocationController>();
            for(int i = 0; i < locs.Length; i++)
            {
                CombineLocation(locs[i].gameObject);
            }
#endif
        }
    }
}
