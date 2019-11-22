using System.Collections.Generic;
using UnityEngine;

namespace WorldGenerator
{
    public class WG_MarchingSquares
    {
        public SquareGrid squareGrid;
        //public MeshFilter walls;
        private List<Vector3> vertices;
        private List<int> triangles;
        private Dictionary<int, List<Triangle>> triangleDictionary;
        private List<List<int>> outlines;
        private HashSet<int> checkedVertices;

        private bool createWalls;
        List<Vector3> wallVertices;
        List<int> wallTriangles;
        private List<Vector2> wallUV;
        private float totalWallLength;

        private float height;
        private float wallHeight;

        //boundary of the constructed zone
        private float minX;
        private float maxX;
        private float minY;
        private float maxY;

        public MeshDataClass GenerateMesh(bool[,] map, bool useException, float _minX, float _maxX, float _minY, float _maxY, float squareSize, float _height, bool invert, bool calculateWalls, float _wallsHeight)
        {
            squareGrid = new SquareGrid(map, squareSize, invert);
            triangleDictionary = new Dictionary<int, List<Triangle>>();
            checkedVertices = new HashSet<int>();
            outlines = new List<List<int>>();
            vertices = new List<Vector3>();
            triangles = new List<int>();
            createWalls = false;
            wallVertices = new List<Vector3>();
            wallTriangles = new List<int>();
            wallUV = new List<Vector2>();
            totalWallLength = 0f;
            height = _height;
            wallHeight = _wallsHeight;
            minX = _minX;
            maxX = _maxX;
            minY = _minY;
            maxY = _maxY;

            for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
            {
                for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
                {
                    TriangulateSquare(squareGrid.squares[x, y], useException);
                }
            }

            MeshDataClass toRetrun = new MeshDataClass {vertices = vertices, triangles = triangles};
            
            if (calculateWalls)
            {
                CreateWallMesh();
            }

            return toRetrun;
        }

        void CreateWallMesh()
        {
            CalculateMeshOutlines();
            float lengthShift = 0f;
            foreach (List<int> outline in outlines)
            {
                if (outline.Count > 0)
                {
                    for (int i = 0; i < outline.Count - 1; i++)
                    {
                        int startIndex = wallVertices.Count;
                        wallVertices.Add(vertices[outline[i]]);
                        wallVertices.Add(vertices[outline[i + 1]]);
                        wallVertices.Add(vertices[outline[i]] + wallHeight * Vector3.up);

                        wallVertices.Add(vertices[outline[i + 1]] + wallHeight * Vector3.up);

                        wallTriangles.Add(startIndex + 0);
                        wallTriangles.Add(startIndex + 2);
                        wallTriangles.Add(startIndex + 3);

                        wallTriangles.Add(startIndex + 3);
                        wallTriangles.Add(startIndex + 1);
                        wallTriangles.Add(startIndex + 0);

                        //add uv data
                        float edgeLength = Vector3.Distance(vertices[outline[i]], vertices[outline[i + 1]]);
                        wallUV.Add(new Vector2(lengthShift, 0.0f));
                        wallUV.Add(new Vector2(lengthShift + edgeLength, 0.0f));
                        wallUV.Add(new Vector2(lengthShift, 1.0f));
                        wallUV.Add(new Vector2(lengthShift + edgeLength, 1.0f));
                        lengthShift += edgeLength;
                    }
                }
            }

            totalWallLength = lengthShift;
            createWalls = true;
        }

        public MeshDataClass GetWallsData()
        {
            if (createWalls)
            {
                MeshDataClass toReturn = new MeshDataClass {vertices = wallVertices, triangles = wallTriangles};
                return toReturn;
            }
            else
            {
                Debug.Log("Terrain generated without walls. Can't return walls mesh data.");
                MeshDataClass toReturn = new MeshDataClass
                {
                    vertices = new List<Vector3>(), triangles = new List<int>()
                };
                return toReturn;
            }
        }

        public float GetWallLength()
        {
            return totalWallLength;
        }

        public Vector2[] GetWallsUV()
        {
            if (createWalls)
            {
                return wallUV.ToArray();
            }
            else
            {
                return new Vector2[0];
            }
        }

        void TriangulateSquare(Square square, bool exceptCase)
        //if exceptCase=true, then in the cases 5 and 10 use other triangulation
        {
            switch (square.configuration)
            {
                case 0:
                    break;

                case 1:
                    MeshFromPoints(square.centreLeft, square.centreBottom, square.bottomLeft);
                    break;
                case 2:
                    MeshFromPoints(square.bottomRight, square.centreBottom, square.centreRight);
                    break;
                case 4:
                    MeshFromPoints(square.topRight, square.centreRight, square.centreTop);
                    break;
                case 8:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
                    break;

                case 3:
                    MeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                    break;
                case 6:
                    MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
                    break;
                case 9:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
                    break;
                case 12:
                    MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreLeft);
                    break;
                case 5:
                    if (exceptCase)
                    {
                        MeshFromPoints(square.topRight, square.centreRight, square.centreTop);
                        MeshFromPoints(square.bottomLeft, square.centreLeft, square.centreBottom);
                    }
                    else
                    {
                        MeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
                    }
                    break;
                case 10:
                    if (exceptCase)
                    {
                        MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
                        MeshFromPoints(square.bottomRight, square.centreBottom, square.centreRight);
                    }
                    else
                    {
                        MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.centreBottom, square.centreLeft);
                    }
                    break;

                case 7:
                    MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                    break;
                case 11:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
                    break;
                case 13:
                    MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
                    break;
                case 14:
                    MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centreBottom, square.centreLeft);
                    break;

                case 15:
                    MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                    checkedVertices.Add(square.topLeft.vertexIndex);
                    checkedVertices.Add(square.topRight.vertexIndex);
                    checkedVertices.Add(square.bottomRight.vertexIndex);
                    checkedVertices.Add(square.bottomLeft.vertexIndex);
                    break;
            }
        }

        void MeshFromPoints(params Node[] points)
        {
            AssignVertices(points);
            if (points.Length >= 3)
            {
                CreateTriangle(points[0], points[1], points[2]);
            }

            if (points.Length >= 4)
            {
                CreateTriangle(points[0], points[2], points[3]);
            }

            if (points.Length >= 5)
            {
                CreateTriangle(points[0], points[3], points[4]);
            }

            if (points.Length >= 6)
            {
                CreateTriangle(points[0], points[4], points[5]);
            }
        }

        void AssignVertices(Node[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].vertexIndex == -1)
                {
                    points[i].vertexIndex = vertices.Count;
                    vertices.Add(points[i].position + Vector3.up * height);
                }
            }
        }

        void CreateTriangle(Node a, Node b, Node c)
        {
            triangles.Add(a.vertexIndex);
            triangles.Add(b.vertexIndex);
            triangles.Add(c.vertexIndex);

            Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
            AddTriangleToDictionary(a.vertexIndex, triangle);
            AddTriangleToDictionary(b.vertexIndex, triangle);
            AddTriangleToDictionary(c.vertexIndex, triangle);
        }

        void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
        {
            if (triangleDictionary.ContainsKey(vertexIndexKey))
            {
                triangleDictionary[vertexIndexKey].Add(triangle);
            }
            else
            {
                List<Triangle> triangleList = new List<Triangle> { triangle };
                triangleDictionary.Add(vertexIndexKey, triangleList);
            }
        }

        bool IsDifferentBoundarySides(Vector3 a, Vector3 b)
        //return true if points a and b lies on different sides of the boundary square, false if on the one side
        {
            if (Mathf.Approximately(a.x, b.x) || Mathf.Approximately(a.z, b.z))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        void CalculateMeshOutlines()
        {
            //WG_Helper.PrintArray(vertices.ToArray());
            for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            {
                if (!checkedVertices.Contains(vertexIndex))
                {
                    int newOulineVertex = GetConnectedOutlineVertex(vertexIndex);
                    if (newOulineVertex != -1)
                    {
                        //if both vertexIndex and newOulineVertex are on boundary, skip the process
                        Vector3 startPoint = vertices[vertexIndex];
                        Vector3 nextPoint = vertices[newOulineVertex];
                        bool isStartBoundary = IsNearBoundary(startPoint);
                        bool isNextBoundary = IsNearBoundary(nextPoint);
                        //Debug.Log(startPoint.ToString() + " " + nextPoint.ToString() + " " + isStartBoundary.ToString() + " " + isNextBoundary.ToString() + " " + IsDifferentBoundarySides(startPoint, nextPoint).ToString());
                        if (!isStartBoundary || !isNextBoundary || IsDifferentBoundarySides(startPoint, nextPoint))
                        {
                            checkedVertices.Add(vertexIndex);

                            List<int> newOutline = new List<int> { vertexIndex };
                            outlines.Add(newOutline);
                            FollowOutline(newOulineVertex, outlines.Count - 1);

                            //check if we break the outline new zone boundary
                            //in this case we should revert the process from the last point (in zone boundary) to the other direction
                            int lastIndex = newOutline[newOutline.Count - 1];
                            //Debug.Log("Last index: " + lastIndex.ToString());
                            if (IsNearBoundary(vertices[lastIndex]))
                            {
                                //Debug.Log("point " + lastIndex.ToString() + " near boundary");
                                //remove vertices from the checked set
                                //WG_Helper.PrintArray(checkedVertices);
                                for (int i = newOutline.Count - 1; i >= 0; i--)
                                {
                                    checkedVertices.Remove(newOutline[i]);
                                }
                                //WG_Helper.PrintArray(checkedVertices);
                                //remove outline
                                outlines.RemoveAt(outlines.Count - 1);

                                //start the process again
                                checkedVertices.Add(lastIndex);
                                List<int> newOtherOutline = new List<int> { lastIndex };
                                outlines.Add(newOtherOutline);
                                newOulineVertex = GetConnectedOutlineVertex(lastIndex);
                                FollowOutline(newOulineVertex, outlines.Count - 1);
                            }
                            else
                            {
                                outlines[outlines.Count - 1].Add(vertexIndex);
                            }

                            //check if we should revert outline array
                            //get triangles incident to the start vertex
                            int startVertex = outlines[outlines.Count - 1][0];
                            List<Triangle> startTriangles = triangleDictionary[startVertex];
                            //find in a triangle the next point in the outline
                            int nextVertex = outlines[outlines.Count - 1][1];
                            bool shouldRevert = true;
                            foreach (Triangle startTriangle in startTriangles)
                            {
                                if ((startTriangle.vertexIndexA == startVertex && startTriangle.vertexIndexB == nextVertex) ||
                                    (startTriangle.vertexIndexB == startVertex && startTriangle.vertexIndexC == nextVertex) ||
                                    (startTriangle.vertexIndexC == startVertex && startTriangle.vertexIndexA == nextVertex))
                                {
                                    shouldRevert = false;
                                    break;
                                }
                            }

                            if (shouldRevert)
                            {
                                outlines[outlines.Count - 1] = WG_Helper.Revert(outlines[outlines.Count - 1]);
                            }
                            
                            
                            //WG_Helper.PrintArray(outlines[outlines.Count - 1].ToArray());
                        }
                    }
                }
            }
        }

        bool IsNearBoundary(Vector3 point)
        {
            return Mathf.Approximately(point.x, minX) || Mathf.Approximately(point.x, maxX) ||
                   Mathf.Approximately(point.z, minY) || Mathf.Approximately(point.z, maxY);
        }

        void FollowOutline(int vertex, int outlineIndex)
        {
            if (!IsNearBoundary(vertices[vertex]))
            {
                //Debug.Log("Follow from " + vertex.ToString());
                outlines[outlineIndex].Add(vertex);
                checkedVertices.Add(vertex);
                int nextVertex = GetConnectedOutlineVertex(vertex);
                //Debug.Log("Find next " + nextVertex.ToString());
                if (nextVertex != -1 && !checkedVertices.Contains(nextVertex))
                {
                    if (!IsNearBoundary(vertices[nextVertex]))
                    {
                        FollowOutline(nextVertex, outlineIndex);
                    }
                    else
                    {
                        outlines[outlineIndex].Add(nextVertex);
                        checkedVertices.Add(nextVertex);
                    }
                }
            }
            else
            {
                outlines[outlineIndex].Add(vertex);
                checkedVertices.Add(vertex);
            }
        }

        int GetConnectedOutlineVertex(int vertex)
        {
            List<Triangle> vertTriangles = triangleDictionary[vertex];
            int candidate = -1;
            foreach (Triangle triangle in vertTriangles)
            {
                for (int j = 0; j < 3; j++)
                {
                    int vertexB = triangle[j];
                    bool potential = false;
                    if (vertexB != vertex && !checkedVertices.Contains(vertexB))
                    {
                        if (IsOutlineEdge(vertex, vertexB))
                        {
                            potential = true;
                        }
                    }

                    if (potential)
                    {
                        bool isNearBoundary = IsNearBoundary(vertices[vertexB]);
                        if (!isNearBoundary || IsDifferentBoundarySides(vertices[vertexB], vertices[vertex]))
                        {
                            return vertexB;
                        }
                        else
                        {
                            candidate = vertexB;
                        }
                    }
                }
            }

            return candidate;
        }

        bool IsOutlineEdge(int vertexA, int vertexB)
        {
            List<Triangle> trianglesA = triangleDictionary[vertexA];
            int trianglesCount = 0;
            foreach (Triangle triangle in trianglesA)
            {
                if (triangle.IsContains(vertexB))
                {
                    trianglesCount++;
                    if (trianglesCount > 1)
                    {
                        break;
                    }
                }
            }

            return trianglesCount == 1;
        }

        void CheckTheEdge(List<EdgeDataClass> edges, int v1, int v2, int triangleIndex)
        {
            bool isFind = false;
            for (int i = 0; i < edges.Count; i++)
            {
                if ((edges[i].vertex01 == v1 && edges[i].vertex02 == v2) || (edges[i].vertex01 == v2 && edges[i].vertex02 == v1))
                {
                    edges[i].triangles.Add(triangleIndex);
                    if (edges[i].triangles.Count > 1)
                    {
                        edges[i].isBoundary = false;
                    }
                    isFind = true;
                    i = edges.Count;
                }
            }

            if (!isFind)
            {
                edges.Add(new EdgeDataClass(v1, v2, triangleIndex));
            }
        }

        //Return indexes of edges in the list which incident to the current vertex
        int[] GetEdgesForVertex(List<EdgeDataClass> edges, int vertex)
        {
            List<int> edgeIndexes = new List<int>();
            for (int e = 0; e < edges.Count; e++)
            {
                EdgeDataClass edge = edges[e];
                if (edge.IsContainsVertex(vertex))
                {
                    edgeIndexes.Add(e);
                }
            }
            return edgeIndexes.ToArray();
        }

        Vector2 GetEdgeNormal(EdgeDataClass edge, Vector3[] _vertices, int[] _triangles)
        {
            Vector3 pos1 = _vertices[edge.vertex01];
            Vector3 pos2 = _vertices[edge.vertex02];
            int v1 = _triangles[3 * edge.triangles[0]];
            int v2 = _triangles[3 * edge.triangles[0] + 1];
            int v3 = _triangles[3 * edge.triangles[0] + 2];
            int v = edge.IsVertexPair(v1, v2) ? v3 : (edge.IsVertexPair(v1, v3) ? v2 : v1);
            Vector3 pos3 = _vertices[v];

            Vector3 edgeDirection = pos2 - pos1;
            Vector3 innerDirection = pos3 - pos1;
            edgeDirection.Normalize();
            innerDirection.Normalize();

            Vector3 vec1 = Vector3.Cross(edgeDirection, innerDirection);
            Vector3 vec2 = Vector3.Cross(edgeDirection, vec1);
            Vector2 normal = new Vector2(vec2.x, vec2.z);
            return normal.normalized;
        }

        //Return array of Vectir2-values for each vertex. This vector is zero for inner vertices and normal direction for boundary vertices
        public Vector2[] GetVertexNormals(Vector3[] _vertices, int[] _triangles)
        {
            //WG_Helper.PrintArray(_vertices);
            //WG_Helper.PrintArray(_triangles);
            //Collect edges
            List<EdgeDataClass> edges = new List<EdgeDataClass>();  // edge is a pair of vertices, 
            int trianglesCount = _triangles.Length / 3;
            for (int i = 0; i < trianglesCount; i++)
            {
                int v1 = _triangles[3 * i];
                int v2 = _triangles[3 * i + 1];
                int v3 = _triangles[3 * i + 2];
                CheckTheEdge(edges, v1, v2, i);
                CheckTheEdge(edges, v2, v3, i);
                CheckTheEdge(edges, v3, v1, i);
            }

            /*for (int i = 0; i < edges.Count; i++)
            {
                Debug.Log(i.ToString() + ": " + edges[i].ToString());
            }*/

            //Iterate throw vertices and check is it boundary or not
            List<Vector2> vertexNormals = new List<Vector2>();
            for (int i = 0; i < _vertices.Length; i++)
            {
                int[] edgeIndexes = GetEdgesForVertex(edges, i);
                Vector2 normal = new Vector2(0, 0);
                for (int j = 0; j < edgeIndexes.Length; j++)
                {
                    EdgeDataClass edge = edges[edgeIndexes[j]];
                    if (edge.isBoundary)
                    {
                        Vector2 edgeNormal = GetEdgeNormal(edge, _vertices, _triangles);
                        normal += edgeNormal;
                        //Debug.Log("vertex " + i.ToString() + ", edge " + edgeIndexes[j].ToString() + ", normal: " + edgeNormal.ToString() + ", result: " + normal.ToString());
                    }
                }
                //Debug.Log(i.ToString() + ": " + WG_Helper.ArrayToString(edgeIndexes) + ", normal: " + normal.ToString());
                vertexNormals.Add(normal.normalized);
            }


            return vertexNormals.ToArray();
        }
    }

}
