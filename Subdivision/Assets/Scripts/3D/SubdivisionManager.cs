using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClassUtils;

public class SubdivisionManager : MonoBehaviour
{
    public List<GameObject> objects;
    public MeshFilter meshFilter;
    public GameObject vertexPrefab;
    public GameObject edgePrefab;
    public GameObject facePrefab;

    private Mesh originalMesh;
    private LoopSubdivision loopSubdivision;
    private CatmullClarkSubdivision catmullSubdivision;
    private KobbeltSubdivision kobbeltSubdivision;
    private ButterflySubdivision butterflySubdivision;
    private List<GameObject> visualizationObjects = new List<GameObject>();

    void Start()
    {
        loopSubdivision = GetComponent<LoopSubdivision>();
        catmullSubdivision = GetComponent<CatmullClarkSubdivision>();
        kobbeltSubdivision = GetComponent<KobbeltSubdivision>();
        butterflySubdivision = GetComponent<ButterflySubdivision>();
        originalMesh = Instantiate(meshFilter.mesh);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) // Loop
        {
            loopSubdivision.Subdivide(meshFilter.mesh);
        }

        if (Input.GetKeyDown(KeyCode.Z)) // Catmull
        {
            // Appel à la subdivision 2
            catmullSubdivision.Subdivide(meshFilter.mesh);
        }

        if (Input.GetKeyDown(KeyCode.E)) // Kobbelt
        {
            kobbeltSubdivision.Subdivide(meshFilter.mesh);
        }

        if (Input.GetKeyDown(KeyCode.Q)) // Butterfly
        {
            butterflySubdivision.Subdivide(meshFilter.mesh);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            // Basculer la visualisation
            ToggleVisualization();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            // Réinitialiser le mesh à l'état d'origine
            meshFilter.mesh = Instantiate(originalMesh);
            ClearVisualization();
        }
    }

    public void Initialize(Mesh mesh, List<Vertex> vertices, List<Edge> edges, List<Face> faces)
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

    public void VisualizePoints(List<Vertex> vertices, List<Edge> edges, List<Face> faces = null)
    {
        ClearVisualization();

        Transform meshTransform = meshFilter.gameObject.transform;

        // Afficher les vertex points
        foreach (Vertex vertex in vertices)
        {
            Vector3 scaledPosition = Vector3.Scale(vertex.position, meshTransform.localScale);
            scaledPosition += meshTransform.localPosition;
            scaledPosition = meshTransform.localRotation * scaledPosition;
            GameObject obj = Instantiate(vertexPrefab, scaledPosition, Quaternion.identity);
            visualizationObjects.Add(obj);
        }

        // Afficher les face points
        if (edges != null)
        {
            foreach (Edge edge in edges)
            {
                Vector3 scaledPosition = Vector3.Scale(edge.edgePoint, meshTransform.localScale);
                scaledPosition += meshTransform.localPosition;
                scaledPosition = meshTransform.localRotation * scaledPosition;
                GameObject obj = Instantiate(facePrefab, scaledPosition, Quaternion.identity);
                visualizationObjects.Add(obj);
            }
        }


        // Afficher les face points
        if (faces != null)
        {
            foreach (Face face in faces)
            {
                Vector3 scaledPosition = Vector3.Scale(face.facePoint, meshTransform.localScale);
                scaledPosition += meshTransform.localPosition;
                scaledPosition = meshTransform.localRotation * scaledPosition;
                GameObject obj = Instantiate(facePrefab, scaledPosition, Quaternion.identity);
                visualizationObjects.Add(obj);
            }
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

    public void ToggleVisualization()
    {
        foreach (var obj in visualizationObjects)
        {
            obj.SetActive(!obj.activeSelf);
        }
    }

    public void DebugStructure(Mesh mesh)
    {
        List<Vertex> vertices = new List<Vertex>();
        List<Edge> edges = new List<Edge>();
        List<Face> faces = new List<Face>();

        Initialize(mesh, vertices, edges, faces);

        Debug.Log("Vertices count : " + vertices.Count + " , Edges count : " + edges.Count + " , Faces count : " + faces.Count);
    }
}

namespace ClassUtils
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
