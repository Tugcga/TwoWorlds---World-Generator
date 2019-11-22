using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Xml.Serialization;
using System.IO;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace WorldGenerator
{
    #if UNITY_EDITOR
    [XmlRoot("location")]
    public class LocationData
    {
        [XmlArray("items")]
        [XmlArrayItem("item")]
        public List<ItemData> items;

        [XmlElement("transform")]
        public TransformData transform;

        [XmlElement("minimap")]
        public MinimapData minimap;

        public void Save(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(LocationData));
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                serializer.Serialize(stream, this);
            }
        }

        public static LocationData Load(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(LocationData));
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                return serializer.Deserialize(stream) as LocationData;
            }
        }
        
        public static LocationData LoadFromText(string text)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(LocationData));
            return serializer.Deserialize(new StringReader(text)) as LocationData;
        }
    }

    public class MinimapData
    {
        [XmlAttribute("name")]
        public string name;

        [XmlAttribute("link")]
        public string link;
    }

    public class TransformData
    {
        [XmlAttribute("position_x")]
        public float positionX;
        [XmlAttribute("position_y")]
        public float positionY;
        [XmlAttribute("position_z")]
        public float positionZ;

        [XmlAttribute("rotation_x")]
        public float rotationX;
        [XmlAttribute("rotation_y")]
        public float rotationY;
        [XmlAttribute("rotation_z")]
        public float rotationZ;

        [XmlAttribute("scale_x")]
        public float scaleX;
        [XmlAttribute("scale_y")]
        public float scaleY;
        [XmlAttribute("scale_z")]
        public float scaleZ;

        public TransformData()
        {

        }

        public TransformData(Transform tfm)
        {
            Vector3 pos = tfm.localPosition;
            Vector3 rot = tfm.eulerAngles;
            Vector3 scl = tfm.localScale;

            positionX = pos.x;
            positionY = pos.y;
            positionZ = pos.z;

            rotationX = rot.x;
            rotationY = rot.y;
            rotationZ = rot.z;

            scaleX = scl.x;
            scaleY = scl.y;
            scaleZ = scl.z;
        }
    }

    public class ItemData
    {
        [XmlAttribute("name")]
        public string name;

        [XmlElement("transform")]
        public TransformData transform;

        [XmlElement("geometry")]
        public MeshData meshData;

        [XmlArray("items")]
        [XmlArrayItem("item")]
        public List<ItemData> items;
    }

    public class MeshData
    {
        [XmlAttribute("mesh_name")]
        public string meshName;

        [XmlAttribute("mesh_link")]
        public string meshLink;

        [XmlAttribute("material_name")]
        public string materialName;

        [XmlAttribute("material_link")]
        public string materialLink;
    }
    #endif

    public class WG_LocationController : MonoBehaviour
    {
        public GameObject groundGO;
        public GameObject wallsGO;
        public int u;
        public int v;
        private Texture2D locationMinimap;
        private string minimapAssetPath;

        private Dictionary<int, NameLinkPath> materialLinks;
        private Dictionary<int, NameLinkPath> textureLinks;
        private Dictionary<int, NameLinkPath> meshLinks;
        private Dictionary<int, NameLinkPath> shaderLinks;

        private string meshAssetFolder;
        private string materialAssetFolder;
        private string textureAssetFolder;
        private string minimapAssetFolder;
        private Shader lmShader;
        private bool isLightmapHDR;

        public void ExportLocation(string locationAssetPath, int minimapSize, Camera minimapCamera, float minimapCameraHeight, string _minimapAssetFolder)
        {
            #if UNITY_EDITOR
            materialLinks = new Dictionary<int, NameLinkPath>();
            textureLinks = new Dictionary<int, NameLinkPath>();
            meshLinks = new Dictionary<int, NameLinkPath>();
            shaderLinks = new Dictionary<int, NameLinkPath>();

            LocationData location = new LocationData
            {
                transform = new TransformData(gameObject.transform), items = new List<ItemData>()
            };

            ExportOneObject(gameObject, location.items);
            
            string minimapName = "minimap_" + u.ToString() + "_" + v.ToString();
            string minimapBundlePath = "minimaps/" + minimapName;
            if (locationMinimap == null)
            {
                minimapAssetFolder = _minimapAssetFolder;
                CreateMinimap(minimapSize, minimapCamera, minimapCameraHeight);
            }
            MinimapData mData = new MinimapData {link = minimapBundlePath, name = locationMinimap.name};
            location.minimap = mData;

            string locationName = "location_" + u.ToString() + "_" + v.ToString();
            string assetPath = locationAssetPath + locationName + ".xml";
            location.Save(assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            //add location asset to the bundle
            AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant("locations/" + locationName, "");
            
            AssetImporter.GetAtPath(minimapAssetPath).SetAssetBundleNameAndVariant(minimapBundlePath, "");

            #endif
        }

#if UNITY_EDITOR
        void ExportOneObject(GameObject obj, List<ItemData> items)
        {
            int childCount = obj.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = obj.transform.GetChild(i);  // this transform in local space of the parent go
                GameObject go = child.gameObject;
                if (go.activeSelf)
                {
                    ItemData newItem = new ItemData {name = go.name, transform = new TransformData(child)};

                    int itemChild = go.transform.childCount;
                    if (itemChild > 0)
                    {
                        newItem.items = new List<ItemData>();
                        ExportOneObject(go, newItem.items);
                    }
                    MeshRenderer objRender = go.GetComponent<MeshRenderer>();
                    MeshFilter objMeshFilter = go.GetComponent<MeshFilter>();
                    if (objRender != null && objMeshFilter != null)
                    {
                        int materialId = ExportMaterial(objRender.sharedMaterial);
                        int meshId = ExportMesh(objMeshFilter.sharedMesh, go);
                        if (materialLinks.ContainsKey(materialId) && meshLinks.ContainsKey(meshId))
                        {
                            newItem.meshData = new MeshData() { meshName = meshLinks[meshId].name, meshLink = meshLinks[meshId].link, materialName = materialLinks[materialId].name, materialLink = materialLinks[materialId].link };
                        }
                        else
                        {
                            Debug.Log("Can not save geometry data of the object " + go.name + ", because it contains unvalid mesh or material.");
                        }
                    }

                    items.Add(newItem);
                }
            }
        }
        #endif

        public int ExportMaterial(Material material)
        {
            int materialId = material.GetInstanceID();
            int shaderId = material.shader.GetInstanceID();
            #if UNITY_EDITOR
            //should we add the shader to the asset bundle
            if (!shaderLinks.ContainsKey(shaderId))
            {
                Shader shader = material.shader;
                string shaderName = "shader_" + shaderId.ToString();
                string shaderLink = "shaders/" + shaderName;
                string assetPath = AssetDatabase.GetAssetPath(shader);
                AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant(shaderLink, "");

                shaderLinks.Add(shaderId, new NameLinkPath() { link = shaderLink, name = shaderName, path = assetPath });
            }
            if (!materialLinks.ContainsKey(materialId))
            {
                string matName = "material_" + materialId.ToString() + "_" + WG_Helper.NormalizeName(material.name);
                string matLink = "materials/" + matName;
                string assetPath = AssetDatabase.GetAssetPath(material);

                //iterate throw parameters of the material and try to find all textures
                Shader shader = material.shader;
                int propsCount = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < propsCount; i++)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string texPropName = ShaderUtil.GetPropertyName(shader, i);
                        Texture tex = material.GetTexture(texPropName);
                        ExportTexture(tex);
                    }
                }
                AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant(matLink, "");

                materialLinks.Add(materialId, new NameLinkPath() {link = matLink, name = material.name, path = assetPath});
            }
            #endif
            return materialId;
        }

        public void ExportTexture(Texture tex)
        {
            #if UNITY_EDITOR
            if (tex != null)
            {
                int texId = tex.GetInstanceID();
                if (!textureLinks.ContainsKey(texId))
                {
                    string texName = "texture_" + texId.ToString() + "_" +
                                     WG_Helper.NormalizeName(tex.name);
                    string texPath = AssetDatabase.GetAssetPath(tex);
                    string texLink = "textures/" + texName;
                    AssetImporter.GetAtPath(texPath).SetAssetBundleNameAndVariant(texLink, "");

                    textureLinks.Add(texId, new NameLinkPath() { link = texLink, name = texName, path = texPath });
                }
            }
            #endif
        }

        public int ExportMesh(Mesh mesh, GameObject obj)
        {
            int meshId = mesh.GetInstanceID();
            #if UNITY_EDITOR
            if (!meshLinks.ContainsKey(meshId))
            {
                string meshName = "mesh_" + meshId.ToString() + "_" + WG_Helper.NormalizeName(mesh.name);
                string meshLink = "meshes/" + meshName;
                string meshPath = AssetDatabase.GetAssetPath(mesh);
                if (meshPath.Length > 0)
                {
                    AssetImporter.GetAtPath(meshPath).SetAssetBundleNameAndVariant(meshLink, "");

                    meshLinks.Add(meshId, new NameLinkPath() { link = meshLink, name = mesh.name, path = meshPath });
                }
                else
                {
                    Debug.Log("Mesh component of the object " + obj.name + " not saved as asset. Save it before exorting scene.");
                }
            }

            #endif
            return meshId;
        }

        public void PrepareLocation(string _meshAssetFolder, 
                                    string _materialAssetFolder, 
                                    string _textureAssetFolder, 
                                    string _minimapAssetFolder, 
                                    Shader _lmShader, 
                                    int minimapSize, 
                                    Camera minimapCamera, 
                                    float minimapCameraHeight, 
                                    bool _isLightmapHDR,
                                    Dictionary<string, string> savedStandardMeshes, 
                                    Dictionary<string, string> savedObjectMeshes)
        {
            #if UNITY_EDITOR
            meshAssetFolder = _meshAssetFolder;
            materialAssetFolder = _materialAssetFolder;
            textureAssetFolder = _textureAssetFolder;
            minimapAssetFolder = _minimapAssetFolder;
            lmShader = _lmShader;
            isLightmapHDR = _isLightmapHDR;
            PrepareObject(gameObject.transform, savedStandardMeshes, savedObjectMeshes);
            CreateMinimap(minimapSize, minimapCamera, minimapCameraHeight);
            #endif
        }
        
        void PrepareObject(Transform root, Dictionary<string, string> savedStandardMeshes, Dictionary<string, string> savedObjectMeshes)
        {
            #if UNITY_EDITOR
            int childCount = root.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = root.GetChild(i);
                GameObject childGO = child.gameObject;
                if (childGO.activeSelf)
                {
                    PrepareMesh(childGO, savedStandardMeshes, savedObjectMeshes);
                    PrepareMaterial(childGO);
                }

                PrepareObject(child, savedStandardMeshes, savedObjectMeshes);
            }
            #endif
        }

        void PrepareMaterial(GameObject go)
        {
            //for material of the geometry we should get lightmap, save it, reassign material with all textures plus lightmap
            #if UNITY_EDITOR
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                //get material
                Material originalMaterial = meshRenderer.sharedMaterial;
                //if material use lightMap texture, then does not save new texture and reassign material
                bool lmSlotExist = IsMaterialContainsLightMap(originalMaterial);
                if (lmSlotExist)
                {
                    Texture t = originalMaterial.GetTexture("_LightMap");
                    if (t == null)
                    {
                        lmSlotExist = false;
                    }
                }
                if (!lmSlotExist)
                {//material does not contains slot for lightmap, so, we should save it and re-assign material
                    //get lightmap
                    string lmTextureAssetPath = "";
                    int lmIndex = meshRenderer.lightmapIndex;
                    bool reAssignMaterial = false;
                    if (lmIndex > -1 && lmIndex < LightmapSettings.lightmaps.Length)
                    {
                        LightmapData data = LightmapSettings.lightmaps[lmIndex];
                        Texture2D lmTexture = data.lightmapColor;
                        WG_Helper.SetTextureReadable(lmTexture, true);
                        //int width = (int)(lmTexture.width * Mathf.Min(1.0f, meshRenderer.lightmapScaleOffset.x));
                        //int height = (int)(lmTexture.height * Mathf.Min(1.0f, meshRenderer.lightmapScaleOffset.y));
                        int width = (int)(lmTexture.width * meshRenderer.lightmapScaleOffset.x);
                        int height = (int)(lmTexture.height * meshRenderer.lightmapScaleOffset.y);

                        //int startX = Mathf.Max(0, (int)(lmTexture.width * meshRenderer.lightmapScaleOffset.z));
                        //int startY = Mathf.Max(0, (int)(lmTexture.height * meshRenderer.lightmapScaleOffset.w));
                        int startX = (int)(lmTexture.width * meshRenderer.lightmapScaleOffset.z);
                        int startY = (int)(lmTexture.height * meshRenderer.lightmapScaleOffset.w);

                        Texture2D newTexture = new Texture2D(width, height, isLightmapHDR ? DefaultFormat.HDR : DefaultFormat.LDR, TextureCreationFlags.MipChain);

                        int mapStartX = Mathf.Max(0, startX);
                        int mapStartY = Mathf.Max(0, startY);
                        int mapEndX = Mathf.Min(lmTexture.width, startX + width);
                        int mapEndY = Mathf.Min(lmTexture.height, startY + height);
                        int mapWidth = mapEndX - mapStartX;
                        int mapHeight = mapEndY - mapStartY;
                        
                        //fill the main rect
                        Color[] lmColorsRaw = lmTexture.GetPixels(mapStartX, mapStartY, mapWidth, mapHeight);
                        Color[] lmColors = WG_Helper.ConverPixelsToLightMap(lmColorsRaw);
                        newTexture.SetPixels(mapStartX - startX, mapStartY - startY, mapWidth, mapHeight, isLightmapHDR ? lmColors : lmColorsRaw);
                        
                        //next fill left area (if we need it)
                        Color[] leftColorsRaw = lmTexture.GetPixels(mapStartX, mapStartY, 1, mapHeight);
                        Color[] leftColors = WG_Helper.ConverPixelsToLightMap(leftColorsRaw);
                        for (int column = 0; column < mapStartX - startX; column++)
                        {
                            newTexture.SetPixels(column, mapStartY - startY, 1, mapHeight, isLightmapHDR ? leftColors : leftColorsRaw);
                        }
                        
                        //next right area
                        Color[] rightColorsRaw = lmTexture.GetPixels(mapEndX - 1, mapStartY, 1, mapHeight);
                        Color[] rightColors = WG_Helper.ConverPixelsToLightMap(rightColorsRaw);
                        for (int column = 0; column < width + startX - mapEndX; column++)
                        {
                            newTexture.SetPixels(mapEndX - startX + column, mapStartY - startY, 1, mapHeight, isLightmapHDR ? rightColors: rightColorsRaw);
                        }

                        //bottom and top
                        Color[] bottomColorsRaw = lmTexture.GetPixels(mapStartX, mapStartY, mapWidth, 1);
                        Color[] bottomColors = WG_Helper.ConverPixelsToLightMap(bottomColorsRaw);
                        for (int row = 0; row < mapStartY - startY; row++)
                        {
                            newTexture.SetPixels(mapStartX - startX, row, mapWidth, 1, isLightmapHDR ? bottomColors : bottomColorsRaw);
                        }
                        Color[] topColorsRaw = lmTexture.GetPixels(mapStartX, mapEndY - 1, mapWidth, 1);
                        Color[] topColors = WG_Helper.ConverPixelsToLightMap(topColorsRaw);
                        for (int row = 0; row < height + startY - mapEndY; row++)
                        {
                            newTexture.SetPixels(mapStartX - startX, mapEndY - startY + row, mapWidth, 1, isLightmapHDR ? topColors : topColorsRaw);
                        }

                        //regions on corners
                        Color leftBootm = isLightmapHDR ? WG_Helper.ConvertColorToLightmap(lmTexture.GetPixel(mapStartX, mapStartY)) : lmTexture.GetPixel(mapStartX, mapStartY);
                        Color leftTop = isLightmapHDR ? WG_Helper.ConvertColorToLightmap(lmTexture.GetPixel(mapStartX, mapEndY - 1)) : lmTexture.GetPixel(mapStartX, mapEndY - 1);
                        Color rightBootm = isLightmapHDR ? WG_Helper.ConvertColorToLightmap(lmTexture.GetPixel(mapEndX - 1, mapStartY)) : lmTexture.GetPixel(mapEndX - 1, mapStartY);
                        Color rightTop = isLightmapHDR ? WG_Helper.ConvertColorToLightmap(lmTexture.GetPixel(mapEndX - 1, mapEndY - 1)) : lmTexture.GetPixel(mapEndX - 1, mapEndY - 1);

                        Color[] leftBottomColors = WG_Helper.CreateArray(leftBootm, (mapStartX - startX) * (mapStartY - startY));
                        Color[] leftTopColors = WG_Helper.CreateArray(leftTop, (mapStartX - startX) * (startY + height - mapEndY));
                        Color[] rightBottomColors = WG_Helper.CreateArray(rightBootm, (startX + width - mapEndX) * (mapStartY - startY));
                        Color[] rightTopColors = WG_Helper.CreateArray(rightTop, (startX + width - mapEndX) * (startY + height - mapEndY));
                        
                        newTexture.SetPixels(0, 0, mapStartX - startX, mapStartY - startY, leftBottomColors);
                        newTexture.SetPixels(0, mapEndY, mapStartX - startX,  startY + height - mapEndY, leftTopColors);
                        //Debug.Log(rightBottomColors.Length.ToString() + " " + (startX + width - mapEndX).ToString() + " " + (mapStartY - startY).ToString());
                        newTexture.SetPixels(mapEndX, 0, startX + width - mapEndX, mapStartY - startY, rightBottomColors);
                        newTexture.SetPixels(mapEndX, mapEndY,  startX + width - mapEndX,  startY + height - mapEndY, rightTopColors);

                        newTexture.Apply();
                        lmTextureAssetPath = SaveTextureAsset(newTexture, isLightmapHDR);
                        WG_Helper.SetTextureLightmap(lmTextureAssetPath, isLightmapHDR);
                        reAssignMaterial = true;
                    }
                    else
                    {
                        Debug.Log("Object " + go.name + " does not have lightmaps or this data is invalid.");
                    }

                    //reassign material if we need it
                    if (reAssignMaterial)
                    {
                        Material newMaterial = new Material(lmShader);
                        //assign all parameters from original material to the new one
                        Shader originalShader = originalMaterial.shader;
                        List<string> newMaterialNames = WG_Helper.GetShaderParameterNames(lmShader);
                        int propsCount = ShaderUtil.GetPropertyCount(originalShader);
                        for (int i = 0; i < propsCount; i++)
                        {
                            string propName = ShaderUtil.GetPropertyName(originalShader, i);
                            if (newMaterialNames.Contains(propName))
                            {
                                ShaderUtil.ShaderPropertyType type = ShaderUtil.GetPropertyType(originalShader, i);
                                if (type == ShaderUtil.ShaderPropertyType.Color)
                                {
                                    newMaterial.SetColor(propName, originalMaterial.GetColor(propName));
                                }
                                else if(type == ShaderUtil.ShaderPropertyType.Float)
                                {
                                    newMaterial.SetFloat(propName, originalMaterial.GetFloat(propName));
                                }
                                else if (type == ShaderUtil.ShaderPropertyType.Range)
                                {
                                    newMaterial.SetFloat(propName, originalMaterial.GetFloat(propName));
                                }
                                else if (type == ShaderUtil.ShaderPropertyType.TexEnv)
                                {
                                    newMaterial.SetTexture(propName, originalMaterial.GetTexture(propName));
                                    newMaterial.SetTextureOffset(propName, originalMaterial.GetTextureOffset(propName));
                                    newMaterial.SetTextureScale(propName, originalMaterial.GetTextureScale(propName));
                                }
                                else if (type == ShaderUtil.ShaderPropertyType.Vector)
                                {
                                    newMaterial.SetVector(propName, originalMaterial.GetVector(propName));
                                }
                            }
                            else
                            {
                                Debug.Log("New material (" + lmShader.name + ") for the object " + go.name + " does not contains property " + propName + ". Skip it.");
                            }
                        }
                        //set gpu instancing
                        newMaterial.enableInstancing = originalMaterial.enableInstancing;
                        //finally assign lightMap texture
                        newMaterial.SetTexture("_LightMap", AssetDatabase.LoadAssetAtPath<Texture2D>(lmTextureAssetPath));
                        //enable all kewords
                        string[] originalKeyWords = originalMaterial.shaderKeywords;
                        for (int s = 0; s < originalKeyWords.Length; s++)
                        {
                            newMaterial.EnableKeyword(originalKeyWords[s]);
                        }
                        //CoreUtils.SetKeyword(newMaterial, "_EMISSION", originalMaterial.IsKeywordEnabled("_EMISSION"));
                        //assign material to the object
                        meshRenderer.sharedMaterial = newMaterial;
                        SaveMaterialAsset(newMaterial);
                    }
                    else
                    {
                        //string matAssetPath = SaveMaterialAsset(originalMaterial);
                        //Debug.Log("Create material asset " + matAssetPath);
                    }
                }
                else
                {
                    SaveMaterialAsset(originalMaterial);
                }
                
            }
            #endif
        }

        //return true if there is a slot for lightmap in the material
        bool IsMaterialContainsLightMap(Material material)
        {
            #if UNITY_EDITOR
            string slotName = "_LightMap";
            if (material == null)
            {
                return false;
            }
            Shader shader = material.shader;
            int propsCount = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < propsCount; i++)
            {
                if (ShaderUtil.GetPropertyName(shader, i) == slotName &&
                    ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    return true;
                }
            }
            #endif
            return false;
        }

        void PrepareMesh(GameObject go, Dictionary<string, string> savedStandardMeshes, Dictionary<string, string> savedObjectMeshes)
        {
            //for mesh we should save the asset if it is no not saved yet
            #if UNITY_EDITOR
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                string meshPath = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                if (meshPath == "Library/unity default resources")
                {//this is standard mesh
                    string sName = meshFilter.sharedMesh.name;
                    string assetPath = "";
                    if (savedStandardMeshes.ContainsKey(sName))
                    {
                        assetPath = savedStandardMeshes[sName];
                    }
                    else
                    {
                        assetPath = SaveStandardMeshAsset(go);
                        savedStandardMeshes.Add(sName, assetPath);
                    }
                    //replace mesh
                    meshFilter.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                }
                else
                {//this is generated or object mesh
                    if (meshPath.Length == 0)
                    {//this mesh is generated and not saved mesh
                        SaveMeshAsset(go);  // simply save the mesh asset
                    }
                    else
                    {//mesh comes from object
                        if (!savedObjectMeshes.ContainsKey(meshPath))
                        {//mesh not saved yet, do it
                            string newMeshPath = SaveMeshAsset(go);
                            savedObjectMeshes.Add(meshPath, newMeshPath);
                        }
                        meshFilter.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(savedObjectMeshes[meshPath]);
                    }
                }
                
            }
            #endif
        }

        void CreateMinimap(int minimapSize, Camera minimapCamera, float minimapCameraHeight)
        {
            RenderTexture renderTexture = new RenderTexture(minimapSize, minimapSize, 16, DefaultFormat.LDR);
            renderTexture.Create();
            minimapCamera.targetTexture = renderTexture;
            Rect rectReadPicture = new Rect(0, 0, minimapSize, minimapSize);

            Vector3 locPosition = gameObject.transform.position;
            minimapCamera.transform.position = new Vector3(locPosition.x, minimapCameraHeight, locPosition.z);
            minimapCamera.Render();

            RenderTexture.active = renderTexture;

            //Create texture
            locationMinimap = new Texture2D(minimapSize, minimapSize);
            locationMinimap.name = "minimap_" + u.ToString() + "_" + v.ToString();

            // Read pixels
            locationMinimap.ReadPixels(rectReadPicture, 0, 0);
            Color[] rawPixels = locationMinimap.GetPixels();
            Color[] processPixels = new Color[rawPixels.Length];
            for (int pi = 0; pi < rawPixels.Length; pi++)
            {
                float a = rawPixels[pi].a;
                processPixels[pi] = new Color(a, a, a, a);
            }
            locationMinimap.SetPixels(processPixels);

            // Clean up
            RenderTexture.active = null;
            renderTexture.Release();

            minimapAssetPath = SaveMinimapAsset(locationMinimap);
            WG_Helper.SetTextureSprite(minimapAssetPath);
        }

        string SaveTextureAsset(Texture2D texture, bool isHdr)
        {
#if UNITY_EDITOR
            string dataPath = WG_Helper.RemoveFirstFolderFromPath(textureAssetFolder) + "lightmap_" + texture.GetInstanceID().ToString() + (isHdr ? ".exr" : ".png");
            string filePath = Application.dataPath + "/" + dataPath;
            if (isHdr)
            {
                byte[] _bytes = texture.EncodeToEXR();
                File.WriteAllBytes(filePath, _bytes);
                AssetDatabase.ImportAsset("Assets/" + dataPath, ImportAssetOptions.ForceUpdate);
                return "Assets/" + dataPath;
            }
            else
            {
                byte[] _bytes = texture.EncodeToPNG();
                File.WriteAllBytes(filePath, _bytes);
                AssetDatabase.ImportAsset("Assets/" + dataPath, ImportAssetOptions.ForceUpdate);
                return "Assets/" + dataPath;
            }
            
            #else
            return "";
            #endif
        }

        string SaveMinimapAsset(Texture2D texture)
        {
            #if UNITY_EDITOR
            byte[] _bytes = texture.EncodeToPNG();
            string dataPath = WG_Helper.RemoveFirstFolderFromPath(minimapAssetFolder) + texture.name + ".png";
            string filePath = Application.dataPath + "/" + dataPath;
            File.WriteAllBytes(filePath, _bytes);
            string assetPath = "Assets/" + dataPath;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return assetPath;
            #else
            return "";
            #endif
        }

        private string SaveMaterialAsset(Material material)
        {
            #if UNITY_EDITOR
            if (material != null)
            {
                string mat_name = "generated_material_" + material.GetInstanceID().ToString();
                string assetPath = materialAssetFolder + mat_name + ".asset";
                if (AssetDatabase.Contains(material) && AssetDatabase.GetAssetPath(material) == assetPath)
                {
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    if (AssetDatabase.Contains(material))
                    {
                        material = Instantiate(material);
                    }

                    AssetDatabase.CreateAsset(material, assetPath);
                }
                return assetPath;
            }
            else
            {
                Debug.Log("Try to save asset of the null material");
            }
            #endif
            return "";
        }

        string SaveMeshAsset(GameObject meshObj)
        {
            #if UNITY_EDITOR
            Mesh mesh = meshObj.GetComponent<MeshFilter>().sharedMesh;
            string meshName = "generated_mesh_" + meshObj.GetInstanceID().ToString();
            string assetPath = meshAssetFolder + meshName + ".asset";
            if (AssetDatabase.Contains(mesh) && AssetDatabase.GetAssetPath(mesh) == assetPath)
            {
                AssetDatabase.SaveAssets();
            }
            else
            {
                if (AssetDatabase.Contains(mesh))
                {
                    mesh = Instantiate(meshObj.GetComponent<MeshFilter>().sharedMesh);
                }

                AssetDatabase.CreateAsset(mesh, assetPath);
            }
            return assetPath;
            #else
            return "";
            #endif
        }

        string SaveStandardMeshAsset(GameObject go)
        {
            #if UNITY_EDITOR
            Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Mesh newMesh = new Mesh();
            newMesh.vertices = mesh.vertices;
            newMesh.triangles = mesh.triangles;
            newMesh.normals = mesh.normals;
            newMesh.uv = mesh.uv;
            newMesh.uv2 = mesh.uv2;

            GameObject newGO = new GameObject() {name = go.name};
            MeshFilter newMeshFilter = newGO.AddComponent<MeshFilter>();
            newMeshFilter.sharedMesh = newMesh;
            SaveMeshAsset(newGO);
            DestroyImmediate(newGO);

            return AssetDatabase.GetAssetPath(newMesh);
            #else
            return "";
            #endif
        }
    }
}