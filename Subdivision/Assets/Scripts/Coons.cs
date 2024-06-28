using System.Collections.Generic;
using DelaunayVoronoi;
using UnityEngine;

public class Coons : MonoBehaviour
{
    #region Public Fields

    public GameObject coonsParent;
    
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
            new(-3, 0),
            new(-2, 1),
            new(-1, 1.5f),
            new(0, 1),
            new(1, .5f),
            new(2, 1),
            new(3, 2 )
        };

        P1 = new List<Point>
        {
            new(-3, 0, 5),
            new(-2, .5f, 5),
            new(-1, 1, 5),
            new(0, 2, 5),
            new(1, 1, 5),
            new(2, .5f, 5),
            new(3, 0, 5)
        };
        
        foreach (var point in P0)
        {
            Instantiate(GameManager.Instance.pointPrefab, point.GetVector(), Quaternion.identity, GameManager.Instance.pointParent.transform);
        }
        
        foreach (var point in P1)
        {
            Instantiate(GameManager.Instance.pointPrefab, point.GetVector(), Quaternion.identity, GameManager.Instance.pointParent.transform);
        }

        for (var i = 0; i < P0.Count-1; i++)
        {
            GameManager.Instance.DrawLine(GameManager.Instance.lineParent, P0[i], P1[i]);
            //CreateSimpleSurface(P0[i+1], P0[i], P1[i], P1[i+1], 10);
        }
        GameManager.Instance.DrawLine(GameManager.Instance.lineParent, P0[^1], P1[^1]);

        Q0 = new List<Point>
        {
            new(-3, 2),
            new(-3, 1, .83f),
            new(-3, .5f, 1.66f),
            new(-3, 0, 2.5f),
            new(-3, .5f, 3.33f),
            new(-3, 1, 4.15f),
            new(-3, 2, 5)
        };

        Q1 = new List<Point>
        {
            new(3, 0),
            new(3, .5f, .83f),
            new(3, 1, 1.66f),
            new(3, 2, 2.5f),
            new(3, 1, 3.33f),
            new(3, .5f, 4.15f),
            new(3, 0, 5)
        };
        
        foreach (var point in Q0)
        {
            Instantiate(GameManager.Instance.pointPrefab, point.GetVector(), Quaternion.identity, GameManager.Instance.pointParent.transform);
        }
        
        foreach (var point in Q1)
        {
            Instantiate(GameManager.Instance.pointPrefab, point.GetVector(), Quaternion.identity, GameManager.Instance.pointParent.transform);
        }

        for (var i = 0; i < Q0.Count-1; i++)
        {
            GameManager.Instance.DrawLine(GameManager.Instance.lineParent, Q0[i], Q1[i]);
            //CreateSimpleSurface(Q0[i+1], Q0[i], Q1[i], Q1[i+1], 10);
        }
        GameManager.Instance.DrawLine(GameManager.Instance.lineParent, Q0[^1], Q1[^1]);
    }

    private void GenerateCoons()
    {
        for (var i = 0; i < Chaikin.Instance.iterations; i++)
        {
            P0 = Chaikin.Instance.ChaikinSubdivision(P0);
            P1 = Chaikin.Instance.ChaikinSubdivision(P1);
            Q0 = Chaikin.Instance.ChaikinSubdivision(Q0);
            Q1 = Chaikin.Instance.ChaikinSubdivision(Q1);
        }

        GenerateCoonsSurface(P0, P1, Q0, Q1, 10);
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
        coonsSurface.transform.parent = coonsParent.transform;
        var meshFilter = coonsSurface.AddComponent<MeshFilter>();
        var meshRenderer = coonsSurface.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;
        meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }
    
    private void CreateSimpleSurface(Point p0, Point p1, Point p2, Point p3)
    {
        // Créer une liste de vecteurs pour les sommets
        List<Vector3> vertices = new List<Vector3>
        {
            p0.GetVector(),
            p1.GetVector(),
            p2.GetVector(),
            p3.GetVector()
        };

        // Créer une liste d'indices pour les triangles
        List<int> indices = new List<int>
        {
            0, 1, 2, // Premier triangle
            0, 2, 3  // Deuxième triangle
        };

        // Créer un nouveau mesh
        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = indices.ToArray()
        };
        mesh.RecalculateNormals();

        // Créer un GameObject pour afficher le mesh
        GameObject simpleSurface = new GameObject("SimpleSurface");
        MeshFilter meshFilter = simpleSurface.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = simpleSurface.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;
        meshRenderer.material = new Material(Shader.Find("Standard"));
    }
    
    private void CreateSimpleSurface(Point p0, Point p1, Point p2, Point p3, int resolution)
    {
        // Interpoler linéairement les courbes de bord
        List<Point> P0 = InterpolatePoints(p0, p1, resolution);
        List<Point> P1 = InterpolatePoints(p3, p2, resolution);
        List<Point> Q0 = InterpolatePoints(p0, p3, resolution);
        List<Point> Q1 = InterpolatePoints(p1, p2, resolution);

        // Générer la surface de Coons
        GenerateCoonsSurface(P0, P1, Q0, Q1, resolution);
    }
    
    private List<Point> InterpolatePoints(Point start, Point end, int resolution)
    {
        List<Point> points = new List<Point>();
        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            points.Add(new Point(
                Mathf.Lerp(start.X, end.X, t),
                Mathf.Lerp(start.Y, end.Y, t),
                Mathf.Lerp(start.Z, end.Z, t)
            ));
        }
        return points;
    }
}
