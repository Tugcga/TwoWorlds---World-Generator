using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

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
            //find all objects with tower or position tag
            WG_PositionBase[] objs = FindObjectsOfType<WG_PositionBase>();
            for(int i = 0; i < objs.Length; i++)
            {
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
                        {//this is location root object, destoy it
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
    }
}
