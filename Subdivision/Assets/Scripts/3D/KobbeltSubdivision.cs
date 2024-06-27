using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClassUtils;

public class KobbeltSubdivision : MonoBehaviour
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

        // Perturber les points de sommet
        PerturbVertexPoints(vertices, edges);

        // Reconnecter les points pour former la nouvelle géométrie
        Mesh newMesh = RebuildMesh(vertices, edges, faces);
        meshFilter.mesh = newMesh;

        subdivisionManager.DebugStructure(meshFilter.mesh);
        subdivisionManager.VisualizePoints(vertices, null, faces);
    }

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

    void PerturbVertexPoints(List<Vertex> vertices, List<Edge> edges)
    {
        foreach (Vertex vertex in vertices)
        {
            Vector3 perturbedPosition = Vector3.zero;
            float alpha = ComputeAlpha(vertex.connectedEdges.Count);
            int boundaryEdgeCount = 0;

            foreach (int edgeIndex in vertex.connectedEdges)
            {
                Edge edge = edges[edgeIndex];

                // Récupérer les positions des sommets de l'arête
                Vector3 neighborVertexPosition = vertices[edge.v1].position == vertex.position ? vertices[edge.v2].position : vertices[edge.v1].position;
                perturbedPosition += neighborVertexPosition;

                if (edge.face2 == -1) // Edge est une bordure
                {
                    boundaryEdgeCount++;
                }
            }
            
            if (boundaryEdgeCount > 0) // Ajuster alpha pour les sommets de bordure
            {
                alpha = ComputeAlpha(boundaryEdgeCount);
            }

            perturbedPosition = (1 - alpha) * vertex.position + (alpha / vertex.connectedEdges.Count) * perturbedPosition;
            vertex.position = perturbedPosition;
        }
    }

    float ComputeAlpha(int n)
    {
        return (1.0f / 9.0f) * (4.0f - 2.0f * Mathf.Cos((2.0f * Mathf.PI) / n));
    }

    Mesh RebuildMesh(List<Vertex> vertices, List<Edge> edges, List<Face> faces)
    {
        Mesh newMesh = new Mesh();

        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        Dictionary<Vector3, int> vertexDict = new Dictionary<Vector3, int>();

        // Ajouter les points de sommet perturbés et enregistrer leurs indices
        foreach (Vertex vertex in vertices)
        {
            if (!vertexDict.ContainsKey(vertex.position))
            {
                vertexDict[vertex.position] = newVertices.Count;
                newVertices.Add(vertex.position);
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

                int v1Index = vertexDict[vertices[v1].position];

                // Récupérer les points de face adjacents
                Vector3 adjacentFacePoint = Vector3.zero;
                bool isBoundary = true;
                foreach (Face adjacentFace in faces)
                {
                    if (adjacentFace != face && adjacentFace.vertices.Contains(v1) && adjacentFace.vertices.Contains(v2))
                    {
                        adjacentFacePoint = adjacentFace.facePoint;
                        isBoundary = false;
                        break;
                    }
                }

                // Si c'est une arête de bordure, utiliser le point de sommet perturbé
                int adjacentFacePointIndex = isBoundary ? v1Index : vertexDict[adjacentFacePoint];

                // Ajouter les triangles formés par les face points et les vertex points
                newTriangles.Add(adjacentFacePointIndex);
                newTriangles.Add(facePointIndex);
                newTriangles.Add(v1Index);
            }
        }

        newMesh.vertices = newVertices.ToArray();
        newMesh.triangles = newTriangles.ToArray();
        newMesh.RecalculateNormals();

        return newMesh;
    }
}


namespace KobbeltUtils
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
