﻿using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace WorldGenerator
{
#if UNITY_EDITOR
    [XmlRoot("global_location")]
    public class GlobalLocation
    {
        [XmlElement("navmesh")] public NavmeshData navmeshData;

        [XmlArray("start_positions")] [XmlArrayItem("position")]
        public List<PositionData> positions;

        [XmlElement("bounds")] public LocationsBounds bounds;

        [XmlElement("size")] public LocationSize locationSize;


        public void Save(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GlobalLocation));
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                serializer.Serialize(stream, this);
            }
        }

        public static GlobalLocation Load(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GlobalLocation));
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                return serializer.Deserialize(stream) as GlobalLocation;
            }
        }

        public static GlobalLocation LoadFromText(string text)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GlobalLocation));
            return serializer.Deserialize(new StringReader(text)) as GlobalLocation;
        }
    }

    public class NavmeshData
    {
        [XmlAttribute("name")]
        public string name;

        [XmlAttribute("link")]
        public string link;
    }

    public class PositionData
    {
        [XmlAttribute("position_x")]
        public float positionX;
        [XmlAttribute("position_y")]
        public float positionY;
        [XmlAttribute("position_z")]
        public float positionZ;
    }

    public class LocationsBounds
    {
        [XmlAttribute("min_u")]
        public float minU;

        [XmlAttribute("min_v")]
        public float minV;

        [XmlAttribute("max_u")]
        public float maxU;

        [XmlAttribute("max_v")]
        public float maxV;
    }

    public class LocationSize
    {
        [XmlAttribute("value")] public float value;
    }
#endif

    public enum LMShaderMode
    {
        LWRP,
        Std
    }

    public class WG_LocationsExporter : MonoBehaviour
    {
        public Transform builder;
        public Camera minimapCamera;
        public int minimapSize = 128;
        public float minimapCameraHeight = 5.0f;
        public Shader lwrpShader;
        public Shader[] stdShaders;
        public LMShaderMode lmMode;
        public NavMeshData localNavMesh;
        public string locationRootName = "Locations";
        public string assetLocationXMLPath = "/GeneratedAssets/Locations_xml/";
        public string assetLocationSOPath = "/GeneratedAssets/Locations_so/";
        public string assetMeshPath = "/GeneratedAssets/Meshes/";
        public string assetMaterialPath = "/GeneratedAssets/Materials/";
        public string assetTexturePath = "/GeneratedAssets/Textures/";
        public string assetNavmeshPath = "/GeneratedAssets/Navmeshes/";
        public string assetMinimapPath = "/GeneratedAssets/Minimaps/";
        public string serverPath = "";
        public bool exportPlayerPositionsInLocations = false;
        public bool exportLightmapHDR = true;
        public Color ldrAmbientCompensation;
        public float ldrLightmapMultiplier;
        public bool clearGeneratedAsssets = true;

        public void ClearAssetFolders()
        {
            ClearGeneratedAssets();
        }

        public void ExportScene()
        {
            if (builder != null)
            {
                for (int i = 0; i < builder.childCount; i++)
                {
                    Transform child = builder.GetChild(i);
                    if (child.name == locationRootName)
                    {
                        ExportScene(child);
                        i = builder.childCount;
                    }
                }
            }
            else
            {
                Debug.Log("Initialize builder object in the inspector.");
            }
        }

        void ExportScene(Transform locationRoot)
        {
            WG_Helper.ClearFolder(Application.dataPath + assetLocationSOPath);

            for (int i = 0; i < locationRoot.childCount; i++)
            {
                Transform loc = locationRoot.GetChild(i);
                WG_LocationController locController = loc.GetComponent<WG_LocationController>();
                
                if (locController != null)
                {
                    locController.ExportLocation("Assets" + assetLocationXMLPath, "Assets" + assetLocationSOPath, minimapSize, minimapCamera, minimapCameraHeight, "Assets" + assetMinimapPath);
                }
            }

#if UNITY_EDITOR
            //export navmesh
            WG_TerrainBuilder builderComponent = builder.gameObject.GetComponent<WG_TerrainBuilder>();
            NavMeshData data = builderComponent.GetNavmeshData();
            string navmeshAssetPath = "";
            if (data != null)
            {
                navmeshAssetPath = "navmeshes/" + data.name;
                AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(data)).SetAssetBundleNameAndVariant(navmeshAssetPath, "");
            }
            else
            {
                if (localNavMesh == null)
                {
                    Debug.Log("Navmesh data is null, can't export it.");
                }
                else
                {
                    Debug.Log("Use local version of the navmesh data object.");
                    data = localNavMesh;
                    navmeshAssetPath = "navmeshes/" + localNavMesh.name;
                    AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(localNavMesh)).SetAssetBundleNameAndVariant(navmeshAssetPath, "");
                }
            }

            //create xml with navmeshlink and starting points
            GlobalLocation globalLocation = new GlobalLocation();
            if (navmeshAssetPath.Length > 0 && data != null)
            {
                globalLocation.navmeshData = new NavmeshData() { link = navmeshAssetPath, name = data.name };
                if (exportPlayerPositionsInLocations)
                {
                    globalLocation.positions = new List<PositionData>();
                    WG_Position[] positions = WG_Helper.FindObjectsOfTypeAll<WG_Position>().ToArray();// FindObjectsOfType<WG_Position>();
                    for (int i = 0; i < positions.Length; i++)
                    {
                        Vector3 pos = positions[i].gameObject.transform.position;
                        IntPair loc = WG_Helper.GetLocationCoordinates(pos, builderComponent.segmentSize);
                        if (loc.u >= builderComponent.segmentsMinX && loc.u <= builderComponent.segmenstMaxX && loc.v >= builderComponent.segmentsMinY && loc.v <= builderComponent.segmenstMaxY)
                        {
                            globalLocation.positions.Add(new PositionData() { positionX = pos.x, positionY = pos.y, positionZ = pos.z });
                        }
                    }

                    if (globalLocation.positions.Count == 0)
                    {
                        Debug.Log("No start positions on the scene. Add default position (0, 0, 0).");
                        globalLocation.positions.Add(new PositionData() { positionX = 0.0f, positionY = 0.0f, positionZ = 0.0f });
                    }
                }


                globalLocation.bounds = new LocationsBounds()
                {
                    minU = builderComponent.segmentsMinX,
                    maxU = builderComponent.segmenstMaxX,
                    minV = builderComponent.segmentsMinY,
                    maxV = builderComponent.segmenstMaxY
                };

                globalLocation.locationSize = new LocationSize() { value = builderComponent.segmentSize };

                string globalLocationName = "global_location";
                string globalLocationAssetPath = "Assets" + assetLocationXMLPath + globalLocationName + ".xml";
                globalLocation.Save(globalLocationAssetPath);
                AssetDatabase.ImportAsset(globalLocationAssetPath, ImportAssetOptions.ForceUpdate);
                AssetImporter.GetAtPath(globalLocationAssetPath).SetAssetBundleNameAndVariant("locations/" + globalLocationName, "");

                //export server data
                SavePlayerPositions(builderComponent);
                SaveCollisionMap(serverPath);
                SaveTowerPositions(builderComponent);

                //also export locations data to SO
                GlobalLocationDataSO globalSO = ScriptableObjectUtility.CreateAsset<GlobalLocationDataSO>("Assets" + assetLocationSOPath, globalLocationName);
                globalSO.minU = builderComponent.segmentsMinX;
                globalSO.maxU = builderComponent.segmenstMaxX;
                globalSO.minV = builderComponent.segmentsMinY;
                globalSO.maxV = builderComponent.segmenstMaxY;

                globalSO.size = builderComponent.segmentSize;

                globalSO.navmeshName = data.name;
                globalSO.navmeshLink = navmeshAssetPath;

                if (exportPlayerPositionsInLocations)
                {
                    globalSO.startPositions = new List<Vector3>();
                    for (int pi = 0; pi < globalLocation.positions.Count; pi++)
                    {
                        globalSO.startPositions.Add(new Vector3(globalLocation.positions[pi].positionX, globalLocation.positions[pi].positionY, globalLocation.positions[pi].positionZ));
                    }
                }

                EditorUtility.SetDirty(globalSO);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                AssetImporter.GetAtPath("Assets" + assetLocationSOPath + globalLocationName + ".asset").SetAssetBundleNameAndVariant("locations_so/" + globalLocationName, "");
            }
#endif

        }

        void SavePlayerPositions(WG_TerrainBuilder builderComponent)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";

            WG_Position[] positions = WG_Helper.FindObjectsOfTypeAll<WG_Position>().ToArray();// FindObjectsOfType<WG_Position>();
            string path = serverPath + "PlayerStartPoints.txt";

            using (StreamWriter sw = File.CreateText(path))
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    Vector3 p = positions[i].transform.position;
                    IntPair loc = WG_Helper.GetLocationCoordinates(p, builderComponent.segmentSize);
                    if (loc.u >= builderComponent.segmentsMinX && loc.u <= builderComponent.segmenstMaxX && loc.v >= builderComponent.segmentsMinY && loc.v <= builderComponent.segmenstMaxY)
                    {
                        sw.Write(p.x.ToString("0.000", nfi));
                        sw.Write(",");
                        sw.Write(p.z.ToString("0.000", nfi));
                        if (i < positions.Length - 1)
                        {
                            sw.Write("|");
                        }
                    }
                }
            }
        }

        //void SaveCollisionMap(NavMeshData data)
        public void SaveCollisionMap(string serverPath)
        {
            List<BoundaryEdge> boundary = WG_Helper.GetNavmeshBoundary();

            if (serverPath != null)
            {
                using (StreamWriter outputFile = new StreamWriter(serverPath + @"CollisionMap.txt"))
                {
                    for (int i = 0; i < boundary.Count; i++)
                    {
                        BoundaryEdge edge = boundary[i];
                        outputFile.Write(WG_Helper.Vectro3ToString(edge.start) + "|" + WG_Helper.Vectro3ToString(edge.end) + (i == boundary.Count - 1 ? "" : "|"));
                        //IntIntClass edge = edgesList[boundaryEdges[i]].GetEdgeVertices();

                        //outputFile.Write(WG_Helper.Vectro3ToString(vertexList[edge.value1].position) + "|" + WG_Helper.Vectro3ToString(vertexList[edge.value2].position) + (i == boundaryEdges.Count - 1 ? "" : "|"));
                    }
                }
            }
        }

        void SaveTowerPositions(WG_TerrainBuilder builderComponent)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";

            WG_Tower[] positions = WG_Helper.FindObjectsOfTypeAll<WG_Tower>().ToArray();// FindObjectsOfType<WG_Tower>();
            string path = serverPath + "Towers.txt";

            using (StreamWriter sw = File.CreateText(path))
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    Vector3 p = positions[i].transform.position;
                    IntPair loc = WG_Helper.GetLocationCoordinates(p, builderComponent.segmentSize);
                    if (loc.u >= builderComponent.segmentsMinX && loc.u <= builderComponent.segmenstMaxX && loc.v >= builderComponent.segmentsMinY && loc.v <= builderComponent.segmenstMaxY)
                    {
                        sw.Write(positions[i].towerType);
                        sw.Write(",");
                        sw.Write(p.x.ToString("0.000", nfi));
                        sw.Write(",");
                        sw.Write(p.z.ToString("0.000", nfi));
                        sw.Write(",");
                        sw.Write(positions[i].towerName);
                        if (i < positions.Length - 1)
                        {
                            sw.Write("|");
                        }
                    }
                }
            }
        }

        public void PrepareScene()
        {
            minimapCamera.orthographicSize = builder.GetComponent<WG_TerrainBuilder>().segmentSize / 2.0f;
            Dictionary<string, string> savedStandardMeshes = new Dictionary<string, string>();  // key - standard mesh name, value - saved asset path
            Dictionary<string, string> savedObjectMeshes = new Dictionary<string, string>();  // key - object asset path, value - mesh asset path
            if (clearGeneratedAsssets)
            {
                ClearGeneratedAssets();
            }
            if (builder != null)
            {
                for (int i = 0; i < builder.childCount; i++)
                {
                    Transform child = builder.GetChild(i);
                    if (child.name == locationRootName)
                    {
                        PrepareScene(child, savedStandardMeshes, savedObjectMeshes);
                        i = builder.childCount;
                    }
                }
            }
            else
            {
                Debug.Log("Initialize builder object in the inspector.");
            }
        }

        void PrepareScene(Transform locationRoot, Dictionary<string, string> savedStandardMeshes, Dictionary<string, string> savedObjectMeshes)
        {
            for (int i = 0; i < locationRoot.childCount; i++)
            {
                Transform loc = locationRoot.GetChild(i);
                WG_LocationController locController = loc.GetComponent<WG_LocationController>();
                if (locController != null)
                {
                    locController.PrepareLocation("Assets" + assetMeshPath, 
                        "Assets" + assetMaterialPath, 
                        "Assets" + assetTexturePath, 
                        "Assets" + assetMinimapPath,
                        lwrpShader, stdShaders, lmMode,
                        minimapSize, minimapCamera, minimapCameraHeight, exportLightmapHDR, ldrAmbientCompensation, ldrLightmapMultiplier,
                        savedStandardMeshes, savedObjectMeshes);
                }
            }

            PrepareNavmesh();
        }

        public void PrepareNavmesh()
        {
            //prepare navmesh
            WG_TerrainBuilder builderComponent = builder.gameObject.GetComponent<WG_TerrainBuilder>();
            NavMeshData data = builderComponent.GetNavmeshData();
            if (data != null)
            {
                SaveNavmeshAsset(data);
                localNavMesh = data;
            }
        }

        void ClearGeneratedAssets()
        {
            //Application.dataPath = .../Assets
            WG_Helper.ClearFolder(Application.dataPath + assetMaterialPath);
            WG_Helper.ClearFolder(Application.dataPath + assetLocationXMLPath);
            WG_Helper.ClearFolder(Application.dataPath + assetLocationSOPath);
            WG_Helper.ClearFolder(Application.dataPath + assetMeshPath);
            WG_Helper.ClearFolder(Application.dataPath + assetTexturePath);
            WG_Helper.ClearFolder(Application.dataPath + assetNavmeshPath);
            WG_Helper.ClearFolder(Application.dataPath + assetMinimapPath);
        }

        void SaveNavmeshAsset(NavMeshData data)
        {
            #if UNITY_EDITOR
            string assetName = "navmesh_" + data.GetInstanceID().ToString();
            string assetPath = "Assets" + assetNavmeshPath + assetName + ".asset";
            if (AssetDatabase.Contains(data) && AssetDatabase.GetAssetPath(data) == assetPath)
            {
                AssetDatabase.SaveAssets();
            }
            else
            {
                if (AssetDatabase.Contains(data))
                {
                    data = Instantiate(data);
                }

                AssetDatabase.CreateAsset(data, assetPath);
            }
            #endif
        }
    }
}
