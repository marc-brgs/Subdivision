using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatmullClarkSubdivision : MonoBehaviour
{
    private MeshFilter meshFilter;

    public GameObject vertexPrefab;
    public GameObject edgePrefab;
    public GameObject facePrefab;
    private List<GameObject> visualizationObjects = new List<GameObject>();

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Subdivide");
            
            Subdivide(meshFilter.mesh);
        }
    }

    void Subdivide(Mesh mesh)
    {
        List<Vertex> vertices = new List<Vertex>();
        List<Edge> edges = new List<Edge>();
        List<Face> faces = new List<Face>();

        // Initialiser les listes de sommets, arêtes et faces à partir du mesh
        Initialize(mesh, vertices, edges, faces);
        Debug.Log("Vertices count : "+ vertices.Count);
        Debug.Log("Edges count : " + edges.Count);
        Debug.Log("Faces count : " + faces.Count);

        // Calculer les points de face
        ComputeFacePoints(faces, vertices);

        // Calculer les points d'arête
        ComputeEdgePoints(edges, vertices, faces);

        // Calculer les points de sommet
        ComputeVertexPoints(vertices, edges, faces);

        // Reconnecter les points pour former la nouvelle géométrie
        Mesh newMesh = RebuildMesh(vertices, edges, faces);
        meshFilter.mesh = newMesh;

        // Afficher les points de visualisation
        VisualizePoints(vertices, edges, faces);
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

    void AddEdge(List<Edge> edges, Dictionary<Edge, int> edgeDict, int v1, int v2, int faceIndex)
    {
        Edge edge = new Edge { v1 = Mathf.Min(v1, v2), v2 = Mathf.Max(v1, v2), face1 = faceIndex, face2 = -1 };
        
        if(!edgeDict.ContainsKey(edge))
        {
            // Define the first face
            edgeDict[edge] = edges.Count;
            edges.Add(edge);
        }
        else
        {
            // Define the other face
            int edgeIndex = edgeDict[edge];
            edges[edgeIndex].face2 = faceIndex;
        }
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

            int n = vertex.connectedEdges.Count; // Nombre d'edges adjacentes <=> nombre de faces adjacentes

            // Q : la moyenne des points de face des faces adjacentes
            foreach (int faceIndex in vertex.connectedFaces)
            {
                facePointsSum += faces[faceIndex].facePoint;
            }
            Vector3 Q = facePointsSum / n;

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
            Vector3 vertexPoint = (Q / n) + (2.0f * R / n) + ((n - 3.0f) * vertex.position / n);
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
                //newTriangles.Add(edgePointIndex);
                //newTriangles.Add(vertexDict[vertices[v2].position]);
                //newTriangles.Add(vertexDict[vertices[v1].position]);
            }
        }

        newMesh.vertices = newVertices.ToArray();
        newMesh.triangles = newTriangles.ToArray();
        newMesh.RecalculateNormals();

        return newMesh;
    }

    /*Mesh RebuildMesh(List<Vertex> vertices, List<Edge> edges, List<Face> faces)
    {
        Mesh newMesh = new Mesh();

        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        Dictionary<Vector3, int> vertexDict = new Dictionary<Vector3, int>();

        foreach (Face face in faces)
        {
            List<int> faceVertices = new List<int>();

            foreach (int vertexIndex in face.vertices)
            {
                Vertex vertex = vertices[vertexIndex];
                if (!vertexDict.ContainsKey(vertex.position))
                {
                    vertexDict[vertex.position] = newVertices.Count;
                    newVertices.Add(vertex.position);
                }
                faceVertices.Add(vertexDict[vertex.position]); // Vertex
            }

            foreach (int edgeIndex in face.edges)
            {
                Edge edge = edges[edgeIndex];
                if (!vertexDict.ContainsKey(edge.edgePoint))
                {
                    vertexDict[edge.edgePoint] = newVertices.Count;
                    newVertices.Add(edge.edgePoint);
                }
                faceVertices.Add(vertexDict[edge.edgePoint]); // Edge point
            }

            if (!vertexDict.ContainsKey(face.facePoint))
            {
                vertexDict[face.facePoint] = newVertices.Count;
                newVertices.Add(face.facePoint);
            }
            faceVertices.Add(vertexDict[face.facePoint]); // Face point

            newTriangles.Add(faceVertices[0]);
            newTriangles.Add(faceVertices[1]);
            newTriangles.Add(faceVertices[2]);
            //newTriangles.Add(faceVertices[0]);
            //newTriangles.Add(faceVertices[2]);
            //newTriangles.Add(faceVertices[3]);
        }

        newMesh.vertices = newVertices.ToArray();
        newMesh.triangles = newTriangles.ToArray();
        newMesh.RecalculateNormals();

        return newMesh;
    }*/

    void VisualizePoints(List<Vertex> vertices, List<Edge> edges, List<Face> faces)
    {
        // Supprimer les objets de visualisation précédents
        foreach (var obj in visualizationObjects)
        {
            Destroy(obj);
        }
        visualizationObjects.Clear();

        // Afficher les vertex points
        foreach (Vertex vertex in vertices)
        {
            GameObject obj = Instantiate(vertexPrefab, vertex.position, Quaternion.identity);
            visualizationObjects.Add(obj);
        }

        // Afficher les edge points
        foreach (Edge edge in edges)
        {
            GameObject obj = Instantiate(edgePrefab, edge.edgePoint, Quaternion.identity);
            visualizationObjects.Add(obj);
        }

        // Afficher les face points
        foreach (Face face in faces)
        {
            GameObject obj = Instantiate(facePrefab, face.facePoint, Quaternion.identity);
            visualizationObjects.Add(obj);
        }
    }
}

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