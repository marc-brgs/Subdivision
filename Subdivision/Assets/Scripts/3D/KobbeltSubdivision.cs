using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KobbeltUtils;

public class KobbeltSubdivision : MonoBehaviour
{
    private MeshFilter meshFilter;

    public GameObject vertexPrefab;
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

        // Calculer les points de face
        ComputeFacePoints(faces, vertices);

        // Perturber les points de sommet
        PerturbVertexPoints(vertices, edges);

        // Reconnecter les points pour former la nouvelle géométrie
        Mesh newMesh = RebuildMesh(vertices, edges, faces);
        meshFilter.mesh = newMesh;

        DebugStructure(meshFilter.mesh);
        VisualizePoints(vertices, faces);
    }

    void Initialize(Mesh mesh, List<Vertex> vertices, List<Edge> edges, List<Face> faces)
    {
        // Dictionnaire pour mapper les sommets dupliqués vers des sommets uniques
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

        // Initialisation des faces
        int[] triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Face face = new Face();
            face.vertices.Add(uniqueVerticesDict[mesh.vertices[triangles[i]]]);
            face.vertices.Add(uniqueVerticesDict[mesh.vertices[triangles[i + 1]]]);
            face.vertices.Add(uniqueVerticesDict[mesh.vertices[triangles[i + 2]]]);

            faces.Add(face);
        }

        // Initialisation des arêtes (pour la perturbation des sommets)
        for (int i = 0; i < faces.Count; i++)
        {
            Face face = faces[i];
            AddEdge(edges, face.vertices[0], face.vertices[1], i);
            AddEdge(edges, face.vertices[1], face.vertices[2], i);
            AddEdge(edges, face.vertices[2], face.vertices[0], i);
        }

        // Mise à jour des arêtes pour lier les sommets et les faces
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

    void AddEdge(List<Edge> edges, int v1, int v2, int faceIndex)
    {
        Edge edge = new Edge { v1 = Mathf.Min(v1, v2), v2 = Mathf.Max(v1, v2), face1 = faceIndex, face2 = -1 };

        int index = edges.FindIndex(e => e.Equals(edge));
        if (index == -1)
        {
            edges.Add(edge);
        }
        else
        {
            edges[index].face2 = faceIndex;
        }
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

                if (edge.face2 == -1)
                {
                    boundaryEdgeCount++;
                }
            }
            if (boundaryEdgeCount > 0)
            {
                // Ajuster alpha pour les sommets de bordure
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

    void VisualizePoints(List<Vertex> vertices, List<Face> faces)
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
        foreach (Face face in faces)
        {
            Vector3 scaledPosition = Vector3.Scale(face.facePoint, transform.localScale);
            scaledPosition += transform.localPosition;
            scaledPosition = transform.localRotation * scaledPosition;
            GameObject obj = Instantiate(facePrefab, scaledPosition, Quaternion.identity);
            visualizationObjects.Add(obj);
        }
    }

    void ClearVisualization()
    {
        // Supprimer les objets de visualisation précédents
        foreach (var obj in visualizationObjects)
        {
            Destroy(obj);
        }
        visualizationObjects.Clear();
    }

    void ToggleVisualization()
    {
        // Supprimer les objets de visualisation précédents
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

        // Initialiser les listes de sommets, arêtes et faces à partir du mesh
        Initialize(mesh, vertices, edges, faces);

        Debug.Log("Vertices count : " + vertices.Count + ", Edges count :" + edges.Count + " , Faces count : " + faces.Count);
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
