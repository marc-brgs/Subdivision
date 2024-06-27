using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClassUtils;

public class CatmullClarkSubdivision : MonoBehaviour
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

        // Calculer les points de face
        ComputeFacePoints(faces, vertices);

        // Calculer les points d'arête
        ComputeEdgePoints(edges, vertices, faces);

        // Calculer les points de sommet
        ComputeVertexPoints(vertices, edges, faces);

        // Reconnecter les points pour former la nouvelle géométrie
        Mesh newMesh = RebuildMesh(vertices, edges, faces);
        meshFilter.mesh = newMesh;

        subdivisionManager.DebugStructure(newMesh);
        subdivisionManager.VisualizePoints(vertices, edges, faces);
    }

    // OK : centroid
    void ComputeFacePoints(List<Face> faces, List<Vertex> vertices)
    {
        foreach (Face face in faces)
        {
            Vector3 facePoint = Vector3.zero;
            foreach (int vertexIndex in face.vertices)
            {
                facePoint += vertices[vertexIndex].position;
            }
            facePoint /= face.vertices.Count;
            face.facePoint = facePoint;
        }
    }

    // OK : pseudo centroid
    void ComputeEdgePoints(List<Edge> edges, List<Vertex> vertices, List<Face> faces)
    {
        foreach (Edge edge in edges)
        {
            Vector3 edgePoint = Vector3.zero;

            // Ajouter les positions des sommets de l'arête
            edgePoint += vertices[edge.v1].position + vertices[edge.v2].position;

            // Ajouter les points de face des faces adjacentes
            edgePoint += faces[edge.face1].facePoint;
            if (edge.face2 != -1)
            {
                edgePoint += faces[edge.face2].facePoint;
                edgePoint /= 4.0f; // Diviser par 4 car il y a 4 points (2 sommets + 2 faces)
            }
            else
            {
                edgePoint /= 3.0f; // Diviser par 3 car il y a 3 points (2 sommets + 1 face)
            }

            edge.edgePoint = edgePoint;
        }
    }

    void ComputeVertexPoints(List<Vertex> vertices, List<Edge> edges, List<Face> faces)
    {
        foreach (Vertex vertex in vertices)
        {
            Vector3 facePointsSum = Vector3.zero;
            Vector3 edgePointsSum = Vector3.zero;

            int n = vertex.connectedEdges.Count; // Nombre de faces adjacentes

            // Q : la moyenne des points de face des faces adjacentes
            foreach (int faceIndex in vertex.connectedFaces)
            {
                facePointsSum += faces[faceIndex].facePoint;
            }
            Vector3 Q = facePointsSum / vertex.connectedFaces.Count;

            // R : la moyenne des points médians des arêtes connectées au sommet
            foreach (int edgeIndex in vertex.connectedEdges)
            {
                Edge edge = edges[edgeIndex];
                Vector3 midpoint = (vertices[edge.v1].position + vertices[edge.v2].position) / 2.0f;
                edgePointsSum += midpoint;
            }
            Vector3 R = edgePointsSum / n;

            // Appliquer la formule de Catmull-Clark pour les nouveaux points de sommet
            // v' = (1/n)Q + (2/n) * R + ((n-3)/n) * v
            Vector3 vertexPoint = (1.0f / n) * Q + (2.0f / n) * R + ((n - 3.0f) / n) * vertex.position;
            vertex.position = vertexPoint;
        }
    }

    Mesh RebuildMesh(List<Vertex> vertices, List<Edge> edges, List<Face> faces)
    {
        Mesh newMesh = new Mesh();

        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        Dictionary<Vector3, int> vertexDict = new Dictionary<Vector3, int>();

        // Ajouter les nouveaux sommets et enregistrer leurs indices
        foreach (Vertex vertex in vertices)
        {
            if (!vertexDict.ContainsKey(vertex.position))
            {
                vertexDict[vertex.position] = newVertices.Count;
                newVertices.Add(vertex.position);
            }
        }

        // Ajouter les points de bord et enregistrer leurs indices
        foreach (Edge edge in edges)
        {
            if (!vertexDict.ContainsKey(edge.edgePoint))
            {
                vertexDict[edge.edgePoint] = newVertices.Count;
                newVertices.Add(edge.edgePoint);
            }
        }

        // Ajouter les points de face et enregistrer leurs indices
        foreach (Face face in faces)
        {
            if (!vertexDict.ContainsKey(face.facePoint))
            {
                vertexDict[face.facePoint] = newVertices.Count;
                newVertices.Add(face.facePoint);
            }
        }

        // Créer les nouveaux triangles
        foreach (Face face in faces)
        {
            int facePointIndex = vertexDict[face.facePoint];

            for (int i = 0; i < face.vertices.Count; i++)
            {
                int v1 = face.vertices[i];
                int v2 = face.vertices[(i + 1) % face.vertices.Count];

                // Trouver l'arête correspondante
                Edge edge = edges.Find(e => (e.v1 == v1 && e.v2 == v2) || (e.v1 == v2 && e.v2 == v1));
                int edgePointIndex = vertexDict[edge.edgePoint];

                // Créer le triangle face point - edge point - vertex point
                newTriangles.Add(vertexDict[vertices[v1].position]); // vertex point içi
                newTriangles.Add(edgePointIndex);
                newTriangles.Add(facePointIndex);

                // Créer le triangle edge point - next vertex point - vertex point
                newTriangles.Add(edgePointIndex);
                newTriangles.Add(vertexDict[vertices[v2].position]);
                newTriangles.Add(facePointIndex);
            }
        }

        newMesh.vertices = newVertices.ToArray();
        newMesh.triangles = newTriangles.ToArray();
        newMesh.RecalculateNormals();

        return newMesh;
    }
}

namespace CatmullUtils
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
        public Vector3 facePoint;
    }
}