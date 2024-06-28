using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClassUtils;

public class LoopSubdivision : MonoBehaviour
{
    public MeshFilter meshFilter;
    private SubdivisionManager subdivisionManager;

    void Start()
    {
        subdivisionManager = GetComponent<SubdivisionManager>();
        meshFilter = subdivisionManager.meshFilter;
    }

    public void Subdivide(Mesh mesh, bool visualisePoints = false)
    {
        List<Vertex> vertices = new List<Vertex>();
        List<Edge> edges = new List<Edge>();
        List<Face> faces = new List<Face>();

        // Initialiser les listes de sommets, arêtes et faces à partir du mesh
        subdivisionManager.Initialize(mesh, vertices, edges, faces);

        // Calculer les points d'arête
        ComputeEdgePoints(faces, edges, vertices);

        // Calculer les points de sommet
        ComputeVertexPoints(vertices, edges);

        // Reconnecter les points pour former la nouvelle géométrie
        Mesh newMesh = RebuildMesh(vertices, edges, faces);
        meshFilter.mesh = newMesh;

        subdivisionManager.DebugStructure(newMesh);
        if (visualisePoints)
        {
            subdivisionManager.VisualizePoints(vertices, edges);
        }
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

                // Calculer le point d'arête
                edgePoint = (3.0f / 8.0f) * (v1 + v2) + (1.0f / 8.0f) * (vLeft + vRight);
            }
            else
            {
                // Si l'arête n'a qu'une seule face, utiliser seulement les deux sommets de l'arête
                edgePoint = (v1 + v2) / 2.0f;
            }

            edge.edgePoint = edgePoint;
        }
    }

    void ComputeVertexPoints(List<Vertex> vertices, List<Edge> edges)
    {
        foreach (Vertex vertex in vertices)
        {
            Vector3 neighborSum = Vector3.zero;
            int n = vertex.connectedEdges.Count;

            foreach (int edgeIndex in vertex.connectedEdges)
            {
                Edge edge = edges[edgeIndex];
                int neighborIndex = edge.v1 == vertices.IndexOf(vertex) ? edge.v2 : edge.v1;
                neighborSum += vertices[neighborIndex].position;
            }

            float alpha;
            if (n == 3)
            {
                alpha = 3.0f / 16.0f;
            }
            else
            {
                float theta = 2.0f * Mathf.PI / n;
                alpha = (1.0f / n) * (5.0f / 8.0f - Mathf.Pow((3.0f / 8.0f) + (1.0f / 4.0f) * Mathf.Cos(theta), 2));
            }
            //float alpha = 3.0f / (8.0f * n); // Proposed by Warren
            Vector3 vertexPoint = (1 - n * alpha) * vertex.position + alpha * neighborSum;
            vertex.position = vertexPoint;
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