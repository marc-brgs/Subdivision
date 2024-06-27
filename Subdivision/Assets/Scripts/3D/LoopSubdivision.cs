using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoopUtils;

public class LoopSubdivision : MonoBehaviour
{
    private MeshFilter meshFilter;

    public GameObject vertexPrefab;
    public GameObject edgePrefab;
    public GameObject facePrefab;
    private List<GameObject> visualizationObjects = new List<GameObject>();

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        DebugStructure(meshFilter.mesh);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            Subdivide(meshFilter.mesh);
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            ToggleVisualization();
        }
    }

    void Subdivide(Mesh mesh, bool visualizeOnly = false)
    {
        List<Vertex> vertices = new List<Vertex>();
        List<Edge> edges = new List<Edge>();
        List<Face> faces = new List<Face>();

        // Initialiser les listes de sommets, arêtes et faces à partir du mesh
        Initialize(mesh, vertices, edges, faces);

        // Calculer les points d'arête
        ComputeEdgePoints(faces, edges, vertices);

        // Calculer les points de sommet
        ComputeVertexPoints(vertices, edges);

        // Reconnecter les points pour former la nouvelle géométrie
        Mesh newMesh = RebuildMesh(vertices, edges, faces);
        meshFilter.mesh = newMesh;

        DebugStructure(newMesh);
        VisualizePoints(vertices, edges);
    }

    void Initialize(Mesh mesh, List<Vertex> vertices, List<Edge> edges, List<Face> faces)
    {
        Dictionary<Vector3, int> uniqueVerticesDict = new Dictionary<Vector3, int>();

        // Initialisation des sommets uniques
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            Vector3 position = mesh.vertices[i];
            if (!uniqueVerticesDict.ContainsKey(position))
            {
                uniqueVerticesDict[position] = vertices.Count;
                vertices.Add(new Vertex { position = position });
            }
        }

        // Initialisation des faces et des arêtes
        int[] triangles = mesh.triangles;
        Dictionary<Edge, int> edgeDict = new Dictionary<Edge, int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Face face = new Face();
            face.vertices.Add(uniqueVerticesDict[mesh.vertices[triangles[i]]]);
            face.vertices.Add(uniqueVerticesDict[mesh.vertices[triangles[i + 1]]]);
            face.vertices.Add(uniqueVerticesDict[mesh.vertices[triangles[i + 2]]]);

            faces.Add(face);

            AddEdge(edges, edgeDict, face.vertices[0], face.vertices[1], faces.Count - 1);
            AddEdge(edges, edgeDict, face.vertices[1], face.vertices[2], faces.Count - 1);
            AddEdge(edges, edgeDict, face.vertices[2], face.vertices[0], faces.Count - 1);
        }

        foreach (Edge edge in edges)
        {
            vertices[edge.v1].connectedEdges.Add(edges.IndexOf(edge));
            vertices[edge.v1].connectedFaces.Add(edge.face1);

            vertices[edge.v2].connectedEdges.Add(edges.IndexOf(edge));
            vertices[edge.v2].connectedFaces.Add(edge.face1);

            if (edge.face2 != -1)
            {
                vertices[edge.v1].connectedFaces.Add(edge.face2);
                vertices[edge.v2].connectedFaces.Add(edge.face2);
            }
        }
    }

    void AddEdge(List<Edge> edges, Dictionary<Edge, int> edgeDict, int v1, int v2, int faceIndex)
    {
        Edge edge = new Edge { v1 = Mathf.Min(v1, v2), v2 = Mathf.Max(v1, v2), face1 = faceIndex, face2 = -1 };

        if (!edgeDict.ContainsKey(edge))
        {
            edgeDict[edge] = edges.Count;
            edges.Add(edge);
        }
        else
        {
            int edgeIndex = edgeDict[edge];
            edges[edgeIndex].face2 = faceIndex;
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

            float alpha = 3.0f / (8.0f * n);
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

    void VisualizePoints(List<Vertex> vertices, List<Edge> edges)
    {
        ClearVisualization();

        // Afficher les vertex points
        foreach (Vertex vertex in vertices)
        {
            Vector3 scaledPosition = Vector3.Scale(vertex.position, transform.localScale);
            scaledPosition += transform.localPosition;
            scaledPosition = transform.localRotation * scaledPosition;
            GameObject obj = Instantiate(vertexPrefab, scaledPosition, Quaternion.identity);
            visualizationObjects.Add(obj);
        }

        // Afficher les face points
        foreach (Edge edge in edges)
        {
            Vector3 scaledPosition = Vector3.Scale(edge.edgePoint, transform.localScale);
            scaledPosition += transform.localPosition;
            scaledPosition = transform.localRotation * scaledPosition;
            GameObject obj = Instantiate(facePrefab, scaledPosition, Quaternion.identity);
            visualizationObjects.Add(obj);
        }
    }

    void ClearVisualization()
    {
        foreach (var obj in visualizationObjects)
        {
            Destroy(obj);
        }
        visualizationObjects.Clear();
    }

    void ToggleVisualization()
    {
        foreach (var obj in visualizationObjects)
        {
            obj.SetActive(!obj.activeSelf);
        }
    }

    void DebugStructure(Mesh mesh)
    {
        List<Vertex> vertices = new List<Vertex>();
        List<Edge> edges = new List<Edge>();
        List<Face> faces = new List<Face>();

        Initialize(mesh, vertices, edges, faces);

        Debug.Log("Vertices count : " + vertices.Count + " , Edges count : " + edges.Count + " , Faces count : " + faces.Count);
    }
}

namespace LoopUtils
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
