using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClassUtils;

public class ButterflySubdivision : MonoBehaviour
{
    public MeshFilter meshFilter;
    public SubdivisionManager subdivisionManager;

    void Start()
    {
        subdivisionManager = GetComponent<SubdivisionManager>();
        meshFilter = subdivisionManager.meshFilter;
        subdivisionManager.DebugStructure(meshFilter.mesh);
    }

    public void Subdivide(Mesh mesh, bool visualizeOnly = false)
    {
        List<Vertex> vertices = new List<Vertex>();
        List<Edge> edges = new List<Edge>();
        List<Face> faces = new List<Face>();

        // Initialiser les listes de sommets, arêtes et faces à partir du mesh
        subdivisionManager.Initialize(mesh, vertices, edges, faces);

        // Calculer les points d'arête
        ComputeEdgePoints(faces, edges, vertices);

        // Reconnecter les points pour former la nouvelle géométrie
        Mesh newMesh = RebuildMesh(vertices, edges, faces);
        meshFilter.mesh = newMesh;

        subdivisionManager.DebugStructure(newMesh);
        subdivisionManager.VisualizePoints(vertices, edges);
    }

    void ComputeEdgePoints(List<Face> faces, List<Edge> edges, List<Vertex> vertices)
    {
        foreach (Edge edge in edges)
        {
            Vector3 v1 = vertices[edge.v1].position;
            Vector3 v2 = vertices[edge.v2].position;
            Vector3 edgePoint;

            if (edge.face2 != -1)
            {
                // Obtenir les autres sommets des faces adjacentes
                Face face1 = faces[edge.face1];
                Face face2 = faces[edge.face2];

                Vector3 vLeft = Vector3.zero;
                Vector3 vRight = Vector3.zero;

                // Trouver vLeft et vRight dans face1 et face2 respectivement
                foreach (int vertexIndex in face1.vertices)
                {
                    if (vertexIndex != edge.v1 && vertexIndex != edge.v2)
                    {
                        vLeft = vertices[vertexIndex].position;
                        break;
                    }
                }

                foreach (int vertexIndex in face2.vertices)
                {
                    if (vertexIndex != edge.v1 && vertexIndex != edge.v2)
                    {
                        vRight = vertices[vertexIndex].position;
                        break;
                    }
                }

                // Calculer les points d'interpolation supplémentaires
                Vector3 vOpposite1 = Vector3.zero, vOpposite2 = Vector3.zero;
                foreach (int i in vertices[edge.v1].connectedFaces)
                {
                    if (i != edge.face1 && i != edge.face2)
                    {
                        foreach (int vertexIndex in faces[i].vertices)
                        {
                            if (vertexIndex != edge.v1 && vertexIndex != edge.v2)
                            {
                                vOpposite1 = vertices[vertexIndex].position;
                                break;
                            }
                        }
                        break;
                    }
                }
                foreach (int i in vertices[edge.v2].connectedFaces)
                {
                    if (i != edge.face1 && i != edge.face2)
                    {
                        foreach (int vertexIndex in faces[i].vertices)
                        {
                            if (vertexIndex != edge.v1 && vertexIndex != edge.v2)
                            {
                                vOpposite2 = vertices[vertexIndex].position;
                                break;
                            }
                        }
                        break;
                    }
                }

                // Calculer le point d'arête
                edgePoint = (v1 + v2) * 0.5f + (vLeft + vRight) * 0.125f - (vOpposite1 + vOpposite2) * 0.0625f;
            }
            else
            {
                // Si l'arête n'a qu'une seule face, utiliser seulement les deux sommets de l'arête
                edgePoint = (v1 + v2) / 2.0f;
            }

            edge.edgePoint = edgePoint;
        }
    }

    Mesh RebuildMesh(List<Vertex> vertices, List<Edge> edges, List<Face> faces)
    {
        Mesh newMesh = new Mesh();

        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        Dictionary<Vector3, int> vertexDict = new Dictionary<Vector3, int>();

        foreach (Vertex vertex in vertices)
        {
            if (!vertexDict.ContainsKey(vertex.position))
            {
                vertexDict[vertex.position] = newVertices.Count;
                newVertices.Add(vertex.position);
            }
        }

        foreach (Edge edge in edges)
        {
            if (!vertexDict.ContainsKey(edge.edgePoint))
            {
                vertexDict[edge.edgePoint] = newVertices.Count;
                newVertices.Add(edge.edgePoint);
            }
        }

        foreach (Face face in faces)
        {
            int v1 = face.vertices[0];
            int v2 = face.vertices[1];
            int v3 = face.vertices[2];

            Edge edge1 = edges.Find(e => (e.v1 == v1 && e.v2 == v2) || (e.v1 == v2 && e.v2 == v1));
            Edge edge2 = edges.Find(e => (e.v1 == v2 && e.v2 == v3) || (e.v1 == v3 && e.v2 == v2));
            Edge edge3 = edges.Find(e => (e.v1 == v3 && e.v2 == v1) || (e.v1 == v1 && e.v2 == v3));

            int e1 = vertexDict[edge1.edgePoint];
            int e2 = vertexDict[edge2.edgePoint];
            int e3 = vertexDict[edge3.edgePoint];

            newTriangles.Add(vertexDict[vertices[v1].position]);
            newTriangles.Add(e1);
            newTriangles.Add(e3);

            newTriangles.Add(vertexDict[vertices[v2].position]);
            newTriangles.Add(e2);
            newTriangles.Add(e1);

            newTriangles.Add(vertexDict[vertices[v3].position]);
            newTriangles.Add(e3);
            newTriangles.Add(e2);

            newTriangles.Add(e1);
            newTriangles.Add(e2);
            newTriangles.Add(e3);
        }

        newMesh.vertices = newVertices.ToArray();
        newMesh.triangles = newTriangles.ToArray();
        newMesh.RecalculateNormals();

        return newMesh;
    }
}

namespace ButteflyUtils
{
    public class Vertex
    {
        public Vector3 position;
        public List<int> connectedEdges = new List<int>();
        public List<int> connectedFaces = new List<int>();
    }

    public class Edge
    {
        public int v1, v2; // Indices des sommets
        public int face1, face2; // Indices des faces
        public Vector3 edgePoint;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Edge other = (Edge)obj;
            return (v1 == other.v1 && v2 == other.v2) || (v1 == other.v2 && v2 == other.v1);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + Mathf.Min(v1, v2).GetHashCode();
            hash = hash * 31 + Mathf.Max(v1, v2).GetHashCode();
            return hash;
        }
    }

    public class Face
    {
        public List<int> vertices = new List<int>(); // Indices des sommets
        public List<int> edges = new List<int>(); // Indices des arêtes
    }
}
