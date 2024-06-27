using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopSubdivision : MonoBehaviour
{
    public MeshFilter meshFilter;
    private Mesh originalMesh;

    private List<Vector3> debugPoints = new List<Vector3>();

    void Start()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (meshFilter != null)
        {
            originalMesh = meshFilter.mesh;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (meshFilter != null && originalMesh != null)
            {
                debugPoints.Clear();
                ComputeEdgePoints(originalMesh);
            }
        }
    }

    void ComputeEdgePoints(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Get the object's scale, position, and rotation
        Vector3 objectScale = transform.lossyScale;
        Vector3 objectPosition = transform.position;
        Quaternion objectRotation = transform.rotation;

        Dictionary<Edge, Vector3> edgeMidpoints = new Dictionary<Edge, Vector3>();

        // Iterate through all triangles
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Get vertex indices for this triangle
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];

            // Calculate midpoints of edges, considering object scale, position, and rotation
            Vector3 mid01 = GetOrCreateMidpoint(edgeMidpoints, vertices, v0, v1, objectScale, objectPosition, objectRotation);
            Vector3 mid12 = GetOrCreateMidpoint(edgeMidpoints, vertices, v1, v2, objectScale, objectPosition, objectRotation);
            Vector3 mid20 = GetOrCreateMidpoint(edgeMidpoints, vertices, v2, v0, objectScale, objectPosition, objectRotation);

            // Debug display (for visualization purposes)
            debugPoints.Add(mid01);
            debugPoints.Add(mid12);
            debugPoints.Add(mid20);
        }
    }

    Vector3 GetOrCreateMidpoint(Dictionary<Edge, Vector3> edgeMidpoints, Vector3[] vertices, int v0, int v1, Vector3 objectScale, Vector3 objectPosition, Quaternion objectRotation)
    {
        Edge edge = new Edge(v0, v1);
        if (!edgeMidpoints.ContainsKey(edge))
        {
            // Calculate midpoint with object scale, position, and rotation applied
            Vector3 midPoint = objectRotation * Vector3.Scale((vertices[v0] + vertices[v1]) * 0.5f, objectScale) + objectPosition;
            edgeMidpoints[edge] = midPoint;
            return midPoint;
        }
        else
        {
            return edgeMidpoints[edge];
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach (Vector3 point in debugPoints)
        {
            Gizmos.DrawSphere(point, 0.5f); // Increase the size if necessary
        }
    }

    struct Edge
    {
        public int v0, v1;

        public Edge(int v0, int v1)
        {
            if (v0 < v1)
            {
                this.v0 = v0;
                this.v1 = v1;
            }
            else
            {
                this.v0 = v1;
                this.v1 = v0;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Edge)
            {
                Edge edge = (Edge)obj;
                return v0 == edge.v0 && v1 == edge.v1;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return v0.GetHashCode() ^ v1.GetHashCode();
        }
    }
}


