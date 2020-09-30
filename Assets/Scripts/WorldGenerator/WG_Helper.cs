using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace WorldGenerator
{
    public struct BoundaryEdge
    {
        public int index;
        public Vector3 start;
        public Vector3 end;

        public int startVertex;
        public int endVertes;
    }

    public struct VectorIntInt
    {
        public Vector2 vector;
        public int value1;
        public int value2;
    }

    [System.Serializable]
    public struct Disc
    {
        public Vector2 center;
        public float radius;
        public bool isNegative;
        public int level;
    }

    public struct FloatBool
    {
        public float floatVal;
        public bool boolVal;
    }

    public struct IntPair
    {
        public int u;
        public int v;

        public override string ToString()
        {
            return "(" + u.ToString() + ", " + v.ToString() + ")";
        }
    }

    public struct IntInt
    {
        public int value01;
        public int value02;

        public override string ToString()
        {
            return "(" + value01.ToString() + ", " + value02.ToString() + ")";
        }
    }

    public class VertexData
    {
        public Vector3 position;
        public List<int> indexes = new List<int>();
    }

    public class IntIntClass
    {
        public int value1;
        public int value2;
        public IntIntClass(int v1, int v2)
        {
            value1 = v1;
            value2 = v2;
        }
    }

    public class EdgeData
    {
        public int v1;
        public int v2;
        public bool isForward;
        public bool isBackfard;

        public EdgeData(int s, int e)
        {
            if (s < e)
            {
                v1 = s;
                v2 = e;
                isForward = true;
                isBackfard = false;
            }
            else
            {
                v1 = e;
                v2 = s;
                isForward = false;
                isBackfard = true;
            }
        }

        public bool IsContainsVertices(int i1, int i2)
        {
            if ((v1 == i1 && v2 == i2) || (v1 == i2 && v2 == i1))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryToAdd(int s, int e)
        {
            if (s == v1 && e == v2)
            {
                isForward = true;
                return true;
            }
            else if (s == v2 && e == v1)
            {
                isBackfard = true;
                return true;
            }
            return false;
        }

        public bool IsBoundary()
        {
            return !(isForward && isBackfard);
        }

        public IntIntClass GetEdgeVertices()
        {
            if (isForward)
            {
                return new IntIntClass(v1, v2);
            }
            else
            {
                return new IntIntClass(v2, v1);
            }
        }
    }

    public class EdgeDataClass
    {
        public int vertex01; //start vertex
        public int vertex02; //end vertex
        public bool isBoundary;
        public List<int> triangles; //indexes of incident triangles

        public EdgeDataClass()
        {
            vertex01 = -1;
            vertex02 = -1;
            isBoundary = true;
            triangles = new List<int>();
        }

        public EdgeDataClass(int v1, int v2, int triangle)
        {
            vertex01 = v1;
            vertex02 = v2;
            isBoundary = true;
            triangles = new List<int> {triangle};
        }

        public bool IsContainsVertex(int v)
        {
            if (v == vertex01 || v == vertex02)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsVertexPair(int v1, int v2)
        {
            if ((v1 == vertex01 && v2 == vertex02) || (v1 == vertex02 && v2 == vertex01))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return "(" + vertex01.ToString() + ", " + vertex02.ToString() + "), triangles: " +
                   WG_Helper.ListToString(triangles) + ", isBoundary: " + isBoundary.ToString();
        }
    }

    public struct NameLinkPath
    {
        public string name;
        public string link;
        public string path;
    }

    struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;
        private readonly int[] vertices;

        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3] { a, b, c };
        }

        public int this[int i] => vertices[i];

        public bool IsContains(int vertex)
        {
            return vertexIndexA == vertex || vertexIndexB == vertex || vertexIndexC == vertex;
        }
    }

    //Class for transfer data of generating mesh
    public class MeshDataClass
    {
        public List<Vector3> vertices;
        public List<int> triangles;
    }

    public class SquareGrid
    {
        public Square[,] squares;

        public SquareGrid(bool[,] map, float squareSize, bool invert)
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];
            for (int x = 0; x < nodeCountX; x++)
            {
                for (int y = 0; y < nodeCountY; y++)
                {
                    Vector3 pos = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, 0, -mapHeight / 2 + y * squareSize + squareSize / 2);
                    controlNodes[x, y] = new ControlNode(pos, invert ? !map[x, y] : map[x, y], squareSize);
                }
            }

            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX - 1; x++)
            {
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
            }
        }
    }

    public class Square
    {
        public ControlNode topLeft, topRight, bottomRight, bottomLeft;
        public Node centreTop, centreRight, centreBottom, centreLeft;
        public int configuration;

        public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft)
        {
            topLeft = _topLeft;
            topRight = _topRight;
            bottomRight = _bottomRight;
            bottomLeft = _bottomLeft;

            centreTop = topLeft.right;
            centreRight = bottomRight.above;
            centreBottom = bottomLeft.right;
            centreLeft = bottomLeft.above;

            if (topLeft.active)
            {
                configuration += 8;
            }

            if (topRight.active)
            {
                configuration += 4;
            }

            if (bottomRight.active)
            {
                configuration += 2;
            }

            if (bottomLeft.active)
            {
                configuration += 1;
            }
        }
    }

    public class Node
    {
        public Vector3 position;
        public int vertexIndex = -1;

        public Node(Vector3 _pos)
        {
            position = _pos;
        }
    }

    public class ControlNode : Node
    {
        public bool active;
        public Node above, right;

        public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos)
        {
            active = _active;
            above = new Node(position + Vector3.forward * squareSize / 2);
            right = new Node(position + Vector3.right * squareSize / 2);
        }
    }

    public enum modeEnum { Infinite, Area };

    public enum noiseTypeEnum { Perlin, Unknown };

    public enum additiveEnum { Additive, Subtractive}

    public class WG_Helper
    {
        public static void PrintArray(bool[,] array)
        {
            for (int x = 0; x < array.GetLength(0); x++)
            {
                StringBuilder sb = new StringBuilder();
                for (int y = 0; y < array.GetLength(1); y++)
                {
                    sb.Append(array[x, y] ? "1 " : "0 ");
                }
                Debug.Log(sb.ToString());
            }
        }

        public static void PrintArray(string[] array)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < array.GetLength(0); i++)
            {
                sb.Append(array[i]);
                sb.Append(", ");
            }
            Debug.Log(sb.ToString());
        }

        public static void PrintArray(int[] array)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i].ToString() + " ");
            }
            Debug.Log(sb.ToString());
        }

        public static void PrintArray(Vector3[] array)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i].ToString() + " ");
            }
            Debug.Log(sb.ToString());
        }

        public static void PrintArray(Vector2[] array)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i].ToString() + " ");
            }
            Debug.Log(sb.ToString());
        }

        public static void PrintArray(HashSet<int> array)
        {
            StringBuilder sb = new StringBuilder();
            foreach (int i in array)
            {
                sb.Append(i.ToString() + " ");
            }
            Debug.Log(sb.ToString());
        }

        public static List<int> Revert(List<int> array)
        {
            List<int> toReturn = new List<int>();
            for (int i = array.Count - 1; i >= 0; i--)
            {
                toReturn.Add(array[i]);
            }
            return toReturn;
        }

        public static string RemoveLastDirectoryFromPath(string fullPath)
        {
            string[] parts = fullPath.Split('/');
            int lastIndex = parts.Length - 1;
            if (parts[parts.Length - 1].Length == 0)
            {
                lastIndex = parts.Length - 2;
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lastIndex; i++)
            {
                sb.Append(parts[i]);
                sb.Append("/");
            }
            return sb.ToString();
        }

        public static int GetIndexInArray(WG_Primitive[] array, string id)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].stringId == id)
                {
                    return i;
                }
            }
            return -1;
        }

        public static Vector3 GetNormal(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            Vector3 u = p1 - p0;
            Vector3 v = p2 - p0;
            Vector3 n = Vector3.Cross(v, u);
            return n.normalized;
        }

        public static Vector3 GetAverage(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Vector3 sum = v0 + v1 + v2 + v3;
            return sum.normalized;
        }

        //path abc/def/... transform to def/...
        public static string RemoveFirstFolderFromPath(string path)
        {
            int pos = path.IndexOf('/');
            if (pos > -1)
            {
                return path.Substring(pos + 1);
            }

            return path;
        }

        public static List<string> GetShaderParameterNames(Shader shader)
        {
            List<string> names = new List<string>();
            #if UNITY_EDITOR
            int propsCount = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < propsCount; i++)
            {
                names.Add(ShaderUtil.GetPropertyName(shader, i));
            }
            #endif
            return names;
        }

        public static string ListToString(List<string> list)
        {
            return String.Join(", ", list.ToArray());
        }

        public static string ListToString(List<int> list)
        {
            List<string> values = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                values.Add(list[i].ToString());
            }
            return String.Join(", ", values.ToArray());
        }

        public static string ArrayToString(int[] array)
        {
            List<string> values = new List<string>();
            for (int i = 0; i < array.Length; i++)
            {
                values.Add(array[i].ToString());
            }

            return String.Join(", ", values.ToArray());
        }

        public static string ArrayToString(Vector3[] array)
        {
            List<string> values = new List<string>();
            for (int i = 0; i < array.Length; i++)
            {
                values.Add(array[i].ToString());
            }

            return String.Join(", ", values.ToArray());
        }

        //replace spaces to "_", and uppercase letters to lowercase letters
        public static string NormalizeName(string name)
        {
            string[] parts = name.Split(' ');
            List<string> normalParts = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string s = parts[i];
                if (s.Length > 0)
                {
                    normalParts.Add(s.ToLower());
                }
            }

            return string.Join("_", normalParts.ToArray());
        }

        public static Texture2D ToTexture2D(Texture texture)
        {
            return Texture2D.CreateExternalTexture(
                texture.width,
                texture.height,
                TextureFormat.RGB24,
                false, false,
                texture.GetNativeTexturePtr());
        }

        public static void SetTextureReadable(Texture2D texture, bool isReadable)
        {
            #if UNITY_EDITOR
            if (null == texture) return;

            string assetPath = AssetDatabase.GetAssetPath(texture);
            TextureImporter tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.isReadable = isReadable;

                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();
            }
            #endif
        }

        public static void SetTextureLightmap(string assetPath, bool isHDR)
        {
            #if UNITY_EDITOR
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                if (isHDR)
                {
                    importer.textureType = TextureImporterType.Lightmap;
                }
                else
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = true;
                }

                importer.wrapMode = TextureWrapMode.Clamp;
                
                importer.npotScale = TextureImporterNPOTScale.ToNearest;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();

            }
            #endif
        }

        public static void SetTextureSprite(string assetPath)
        {
            #if UNITY_EDITOR
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                
                importer.npotScale = TextureImporterNPOTScale.ToNearest;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();
            }
            #endif
        }

        public static float ApplyGammaCorrection(float value, float gamma)
        {
            return Mathf.Pow(value, 1.0f / gamma);
        }

        public static Color ConvertColorToLightmap(Color color)
        {
            return color;
            /*return new Color(WG_Helper.ApplyGammaCorrection(color.r, 0.4545f),
                WG_Helper.ApplyGammaCorrection(color.g, 0.4545f),
                WG_Helper.ApplyGammaCorrection(color.b, 0.4545f),
                color.a);*/
        }

        public static Color[] CreateArray(Color value, int length)
        {
            if (length <= 0)
            {
                return new Color[0];
            }

            Color[] toReturn = new Color[length];
            for (int i = 0; i < length; i++)
            {
                toReturn[i] = value;
            }
            return toReturn;
        }

        public static Color[] ConverPixelsToLightMap(Color[] array, bool debug = false)
        {
            Color[] toReturn = new Color[array.Length];
            Color randomColor = Random.ColorHSV();
            for (int i = 0; i < toReturn.Length; i++)
            {
                if (debug)
                {
                    toReturn[i] = randomColor;
                }
                else
                {
                    toReturn[i] = WG_Helper.ConvertColorToLightmap(array[i]);
                }
            }

            return toReturn;
        }

        //path in the form .../abc/ and the method delete all files and folders inside this folder
        public static void ClearFolder(string path)
        {
            #if UNITY_EDITOR
            DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
            #endif
        }

        public static int GetSegmentIndex(int posU, int posV)
        {
            int absPosU = Mathf.Abs(posU);
            int absPosV = Mathf.Abs(posV);
            int squareIndex = Mathf.Max(absPosU, absPosV);
            int segmentIndex = -1;
            if (squareIndex == 0)
            {
                segmentIndex = 0;
            }
            else
            {
                int startSquareIndex = (2 * squareIndex - 1) * (2 * squareIndex - 1);
                if (posU == 0 && posV != 0)
                {
                    segmentIndex = posV > 0 ? startSquareIndex + 6 * squareIndex : startSquareIndex + 2 * squareIndex;
                }
                else if (posU != 0 && posV == 0)
                {
                    segmentIndex = posU > 0 ? startSquareIndex + 4 * squareIndex : startSquareIndex;
                }
                else if (posU < 0 && posV < 0)
                {
                    segmentIndex = startSquareIndex + absPosV + (absPosV > absPosU ? absPosV - absPosU : 0);
                }
                else if (posU < 0 && posV > 0)
                {
                    segmentIndex = startSquareIndex + 6 * squareIndex + absPosU + (absPosU > absPosV ? absPosU - absPosV : 0);
                }
                else if (posU > 0 && posV < 0)
                {
                    segmentIndex = startSquareIndex + 2 * squareIndex + posU + (absPosU > absPosV ? absPosU - absPosV : 0);
                }
                else if (posU > 0 && posV > 0)
                {
                    segmentIndex = startSquareIndex + 4 * squareIndex + posV + (absPosV > absPosU ? absPosV - absPosU : 0);
                }
                else
                {
                    Debug.Log("It's impossible!");
                }
            }
            return segmentIndex;
        }

        public static int GetSegmentNumber(string name)
        {
            string[] parts = name.Split('_');
            if (parts.Length == 2)
            {
                string nString = parts[1];
                int n = -1;
                bool isParsed = System.Int32.TryParse(nString, out n);
                if (isParsed)
                {
                    return n;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }
        }

        static int GetCoordinate(float value, float size)
        {
            float f = value / size + (value > 0 ? 1 : -1) * 0.5f;
            return (int)(Math.Truncate(f));
        }

        //return pair (u, v) for location, which contains given position
        public static IntPair GetLocationCoordinates(Vector3 position, float size)
        {
            return new IntPair() {u = GetCoordinate(position.x, size), v = GetCoordinate(position.z, size)};
        }

        public static IntPair GetUVFromIndex(int index)
        {
            //IntPair toReturn = new IntPair();
            IntPair toReturn = new IntPair {u = 0, v = 0};
            int curentN = 0;
            bool isFind = false;
            while (!isFind)
            {
                if (index < (2 * curentN + 1) * (2 * curentN + 1))
                {
                    isFind = true;
                }
                else
                {
                    curentN++;
                }
            }
            if (curentN == 0)
            {
                toReturn.u = 0;
                toReturn.v = 0;
                return toReturn;
            }
            else
            {
                int s = (2 * curentN - 1) * (2 * curentN - 1);
                if (index >= s && index < s + curentN)
                {
                    toReturn.u = -1 * curentN;
                    toReturn.v = s - index;
                }
                else if (index >= s + curentN && index < s + 3 * curentN)
                {
                    toReturn.u = index - s - 2 * curentN;
                    toReturn.v = -1 * curentN;
                }
                else if (index >= s + 3 * curentN && index < s + 5 * curentN)
                {
                    toReturn.u = curentN;
                    toReturn.v = index - s - 4 * curentN;
                }
                else if (index >= s + 5 * curentN && index < s + 7 * curentN)
                {
                    toReturn.u = -1 * (index - s - 6 * curentN);
                    toReturn.v = curentN;
                }
                else
                {
                    toReturn.u = -1 * curentN;
                    toReturn.v = -1 * (index - s - 8 * curentN);
                }

                return toReturn;
            }
        }

        public static void DrawText(GUISkin guiSkin, string text, Vector3 position, Color? color = null, int fontSize = 0, float yOffset = 0)
        {
            #if UNITY_EDITOR
            var prevSkin = GUI.skin;
            if (guiSkin == null)
                Debug.LogWarning("editor warning: guiSkin parameter is null");
            else
                GUI.skin = guiSkin;

            GUIContent textContent = new GUIContent(text);

            GUIStyle style = (guiSkin != null) ? new GUIStyle(guiSkin.GetStyle("Label")) : new GUIStyle();
            if (color != null)
                style.normal.textColor = (Color)color;
            if (fontSize > 0)
                style.fontSize = fontSize;

            Vector2 textSize = style.CalcSize(textContent);
            Vector3 screenPoint = Camera.current.WorldToScreenPoint(position);

            if (screenPoint.z > 0)
            {
                var worldPosition = Camera.current.ScreenToWorldPoint(new Vector3(screenPoint.x - textSize.x * 0.5f, screenPoint.y + textSize.y * 0.5f + yOffset, screenPoint.z));
                Handles.Label(worldPosition, textContent, style);
            }
            GUI.skin = prevSkin;
            #endif
        }

        public static bool IsInside(Vector2 point, Disc disc)
        {
            return Vector2.Distance(point, disc.center) < disc.radius;
        }

        public static string Vectro3ToString(Vector3 vector)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            return vector.x.ToString("0.000", nfi) + "," + vector.z.ToString("0.000", nfi);
        }

        static bool IsArrayContains(int value, List<int> array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                if (array[i] == value)
                {
                    return true;
                }
            }
            return false;
        }

        public static int GetVertexIndex(int originalIndex, List<VertexData> vertices)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                if (IsArrayContains(originalIndex, vertices[i].indexes))
                {
                    return i;
                }
            }
            return -1;
        }

        public static bool IsPointOnTheEdge(Vector3 point, Vector3 start, Vector3 end)
        {
            //check the start and end positions
            if (Vector3.Distance(point, start) < 0.01f || Vector3.Distance(point, end) < 0.01f)
            {
                return false;
            }
            else
            {
                //calculate two dot products: (sp, se) and (ep, es), both of them should be close to 1
                float d1 = Vector3.Dot((end - start).normalized, (point - start).normalized);
                float d2 = Vector3.Dot((start - end).normalized, (point - end).normalized);
                if (Math.Abs(d1 - 1.0f) < 0.00001f && Math.Abs(d2 - 1.0f) < 0.00001f)
                {
                    return true;
                }
                return false;
            }
        }

        public static bool IsEdgesCollinear(Vector3 pStart, Vector3 pEnd, Vector3 qStart, Vector3 qEnd)
        {
            Vector3 pDirection = (pEnd - pStart).normalized;
            Vector3 qDirection = (qEnd - qStart).normalized;
            if (Math.Abs(Math.Abs(Vector3.Dot(pDirection, qDirection)) - 1.0f) < 0.0001f)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsPointsClose(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) < 0.0001f;
        }

        public static List<BoundaryEdge> GetNavmeshBoundary()
        {
            //copy algorithm 
            NavMeshTriangulation triangulatedNavMesh = NavMesh.CalculateTriangulation();
            Vector3[] originalVertices = triangulatedNavMesh.vertices;
            int[] originalIndexes = triangulatedNavMesh.indices;
            float weldValue = 0.01f;
            List<VertexData> vertexList = new List<VertexData>();
            for (int i = 0; i < originalVertices.Length; i++)
            {
                Vector3 vPos = originalVertices[i];
                bool isNew = true;
                //Try to find this vertex on vertexList
                for (int vIndex = 0; vIndex < vertexList.Count; vIndex++)
                {
                    VertexData v = vertexList[vIndex];
                    if (Vector3.Distance(vPos, v.position) < weldValue)
                    {//i-th vertex placed in the same position as vIndex in the list. Add it index
                        v.indexes.Add(i);
                        vIndex = vertexList.Count;
                        isNew = false;
                    }
                }
                if (isNew)
                {
                    VertexData newVertex = new VertexData();
                    newVertex.position = vPos;
                    newVertex.indexes = new List<int>();
                    newVertex.indexes.Add(i);
                    vertexList.Add(newVertex);
                }
            }
            List<EdgeData> edgesList = new List<EdgeData>();
            int originalTrianglesCount = originalIndexes.Length / 3;
            for (int i = 0; i < originalTrianglesCount; i++)
            {
                int i1 = WG_Helper.GetVertexIndex(originalIndexes[3 * i], vertexList);
                int i2 = WG_Helper.GetVertexIndex(originalIndexes[3 * i + 1], vertexList);
                int i3 = WG_Helper.GetVertexIndex(originalIndexes[3 * i + 2], vertexList);
                bool isFindI12 = false;
                bool isFindI23 = false;
                bool isFindI31 = false;
                for (int eIndex = 0; eIndex < edgesList.Count; eIndex++)
                {
                    isFindI12 = edgesList[eIndex].TryToAdd(i1, i2) || isFindI12;
                    isFindI23 = edgesList[eIndex].TryToAdd(i2, i3) || isFindI23;
                    isFindI31 = edgesList[eIndex].TryToAdd(i3, i1) || isFindI31;
                }
                if (!isFindI12)
                {
                    EdgeData newEdge = new EdgeData(i1, i2);
                    edgesList.Add(newEdge);
                }
                if (!isFindI23)
                {
                    EdgeData newEdge = new EdgeData(i2, i3);
                    edgesList.Add(newEdge);
                }
                if (!isFindI31)
                {
                    EdgeData newEdge = new EdgeData(i3, i1);
                    edgesList.Add(newEdge);
                }
            }

            //filter boundary edges, if it contains vertex of the other boundary edge inside it
            List<int> boundaryIndexes = new List<int>();
            for (int i = 0; i < edgesList.Count; i++)
            {
                if (edgesList[i].IsBoundary())
                {
                    boundaryIndexes.Add(i);
                }
            }

            bool isUpdate = true;
            while (isUpdate)
            {
                isUpdate = false;
                int i = 0;
                while (i < boundaryIndexes.Count)
                {
                    IntInt currentEdge;
                    currentEdge.value01 = edgesList[boundaryIndexes[i]].GetEdgeVertices().value1;
                    currentEdge.value02 = edgesList[boundaryIndexes[i]].GetEdgeVertices().value2;
                    Vector3 currentStart = vertexList[currentEdge.value01].position;
                    Vector3 currentEnd = vertexList[currentEdge.value02].position;
                    if (Vector3.Distance(currentStart, currentEnd) < 0.01f)
                    {
                        boundaryIndexes.RemoveAt(i);
                        i = boundaryIndexes.Count + 1;
                        isUpdate = true;
                    }
                    else
                    {
                        int j = 0;
                        while (j < boundaryIndexes.Count)
                        {
                            IntInt testEdge;
                            testEdge.value01 = edgesList[boundaryIndexes[j]].GetEdgeVertices().value1;
                            testEdge.value02 = edgesList[boundaryIndexes[j]].GetEdgeVertices().value2;
                            Vector3 testStart = vertexList[testEdge.value01].position;
                            Vector3 testEnd = vertexList[testEdge.value02].position;
                            if (Vector3.Distance(testStart, testEnd) < 0.01f)
                            {
                                boundaryIndexes.RemoveAt(j);
                                j = boundaryIndexes.Count + 1;
                                i = boundaryIndexes.Count + 1;
                                isUpdate = true;
                            }
                            else if(i != j && boundaryIndexes[i] < boundaryIndexes[j])
                            {
                                if (IsEdgesCollinear(currentStart, currentEnd, testStart, testEnd))
                                {
                                    if (IsPointsClose(currentStart, testEnd) && IsPointOnTheEdge(testStart, currentStart, currentEnd))
                                    {
                                        EdgeData ith = new EdgeData(testEdge.value01, currentEdge.value02);
                                        EdgeData jth = new EdgeData(currentEdge.value01, testEdge.value02);
                                        edgesList[boundaryIndexes[i]] = ith;
                                        edgesList[boundaryIndexes[j]] = jth;
                                        i = boundaryIndexes.Count + 1;
                                        j = boundaryIndexes.Count + 1;
                                        isUpdate = true;
                                    }
                                    else if (IsPointsClose(currentEnd, testStart) && IsPointOnTheEdge(testEnd, currentStart, currentEnd))
                                    {
                                        EdgeData ith = new EdgeData(currentEdge.value01, testEdge.value02);
                                        EdgeData jth = new EdgeData(testEdge.value01, currentEdge.value02);
                                        edgesList[boundaryIndexes[i]] = ith;
                                        edgesList[boundaryIndexes[j]] = jth;
                                        i = boundaryIndexes.Count + 1;
                                        j = boundaryIndexes.Count + 1;
                                        isUpdate = true;
                                    }
                                    else if(IsPointsClose(currentStart, testEnd) && IsPointOnTheEdge(currentEnd, testStart, testEnd))
                                    {
                                        EdgeData ith = new EdgeData(currentEdge.value01, testEdge.value02);
                                        EdgeData jth = new EdgeData(testEdge.value01, currentEdge.value02);
                                        edgesList[boundaryIndexes[i]] = ith;
                                        edgesList[boundaryIndexes[j]] = jth;
                                        i = boundaryIndexes.Count + 1;
                                        j = boundaryIndexes.Count + 1;
                                        isUpdate = true;
                                    }
                                    else if(IsPointsClose(currentEnd, testStart) && IsPointOnTheEdge(currentStart, testStart, testEnd))
                                    {
                                        EdgeData ith = new EdgeData(testEdge.value01, currentEdge.value02);
                                        EdgeData jth = new EdgeData(currentEdge.value01, testEdge.value02);
                                        edgesList[boundaryIndexes[i]] = ith;
                                        edgesList[boundaryIndexes[j]] = jth;
                                        i = boundaryIndexes.Count + 1;
                                        j = boundaryIndexes.Count + 1;
                                        isUpdate = true;
                                    }
                                    else if(IsPointsClose(currentEnd, testStart) && IsPointsClose(currentStart, testEnd))
                                    {
                                        EdgeData ith = new EdgeData(currentEdge.value01, currentEdge.value01);
                                        EdgeData jth = new EdgeData(testEdge.value01, testEdge.value01);
                                        edgesList[boundaryIndexes[i]] = ith;
                                        edgesList[boundaryIndexes[j]] = jth;
                                        i = boundaryIndexes.Count + 1;
                                        j = boundaryIndexes.Count + 1;
                                        isUpdate = true;
                                    }
                                }
                            }

                            j++;
                        }
                    }
                    i++;
                }
            }

            //copy to the output
            List<BoundaryEdge> toReturn = new List<BoundaryEdge>();
            for (int i = 0; i < boundaryIndexes.Count; i++)
            {
                int index = boundaryIndexes[i];
                IntIntClass edge = edgesList[index].GetEdgeVertices();
                BoundaryEdge newEdge = new BoundaryEdge();
                newEdge.index = index;
                newEdge.start = vertexList[edge.value1].position;
                newEdge.end = vertexList[edge.value2].position;
                newEdge.startVertex = edge.value1;
                newEdge.endVertes = edge.value2;
                toReturn.Add(newEdge);
            }
            return toReturn;
        }
    }
}
