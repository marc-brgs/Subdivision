using System.Collections.Generic;
using DelaunayVoronoi;
using UnityEngine;

public class Coons : MonoBehaviour
{
    #region Public Fields

    [Range(0,5)]
    public int iterations = 1;
    
    public static Coons Instance;

    #endregion

    #region Private Fields

    private List<Point> P0 = new ();
    private List<Point> P1 = new ();
    private List<Point> Q0 = new ();
    private List<Point> Q1 = new ();

    #endregion
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }
    
    private void Start()
    {
        InitializeCoonsCurves();
        GenerateCoons();
    }
    
    private void InitializeCoonsCurves()
    {
        P0 = new List<Point>
        {
            new Point(-3, 0, 0),
            new Point(-2, 2, 0),
            new Point(-1, 4, 0),
            new Point(0, 2, 0),
            new Point(1, 0, 0),
            new Point(2, 2, 0),
            new Point(3, 4, 0)
        };

        foreach (var point in P0)
        {
            Instantiate(GameManager.Instance.pointPrefab, point.GetVector(), Quaternion.identity);
        }

        P1 = new List<Point>
        {
            new Point(0, 0, 3),
            new Point(1, 0, 3),
            new Point(2, 0, 3),
            new Point(3, 0, 3),
            new Point(4, 0, 3),
            new Point(5, 0, 3),
            new Point(6, 0, 3)
        };

        Q0 = new List<Point>
        {
            new Point(0, 0, 0),
            new Point(0, 1, 0.5f),
            new Point(0, 2, 1),
            new Point(0, 3, 3)
        };

        Q1 = new List<Point>
        {
            new Point(3, 0, 0),
            new Point(3, 1, 0.5f),
            new Point(3, 2, 1),
            new Point(3, 3, 3)
        };
    }

    private void GenerateCoons()
    {
        for (var i = 0; i < iterations; i++)
        {
            P0 = Chaikin.Instance.ChaikinSubdivision(P0);
            P1 = Chaikin.Instance.ChaikinSubdivision(P1);
            Q0 = Chaikin.Instance.ChaikinSubdivision(Q0);
            Q1 = Chaikin.Instance.ChaikinSubdivision(Q1);
        }

        GenerateCoonsSurface(P0, P1, Q0, Q1, 5);
    }
    
    private void GenerateCoonsSurface(List<Point> P0, List<Point> P1, List<Point> Q0, List<Point> Q1, int resolution)
    {
        var vertices = new List<Vector3>();
        var indices = new List<int>();

        for (int u = 0; u <= resolution; u++)
        {
            float U = u / (float)resolution;

            for (int v = 0; v <= resolution; v++)
            {
                float V = v / (float)resolution;

                Vector3 pointOnSurface = 
                    (1 - U) * Q0[v].GetVector() + 
                    U * Q1[v].GetVector() + 
                    (1 - V) * P0[u].GetVector() + 
                    V * P1[u].GetVector() - 
                    (1 - U) * (1 - V) * Q0[0].GetVector() - 
                    U * (1 - V) * Q1[0].GetVector() - 
                    (1 - U) * V * Q0[resolution].GetVector() - 
                    U * V * Q1[resolution].GetVector();

                vertices.Add(pointOnSurface);
            }
        }

        for (int u = 0; u < resolution; u++)
        {
            for (int v = 0; v < resolution; v++)
            {
                int i0 = u * (resolution + 1) + v;
                int i1 = i0 + 1;
                int i2 = i0 + (resolution + 1);
                int i3 = i2 + 1;

                indices.Add(i0);
                indices.Add(i2);
                indices.Add(i1);

                indices.Add(i1);
                indices.Add(i2);
                indices.Add(i3);
            }
        }

        var mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = indices.ToArray()
        };
        mesh.RecalculateNormals();

        var coonsSurface = new GameObject("CoonsSurface");
        var meshFilter = coonsSurface.AddComponent<MeshFilter>();
        var meshRenderer = coonsSurface.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;
        meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }
}
