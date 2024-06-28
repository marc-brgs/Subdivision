using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelaunayVoronoi;
using UnityEngine;

public class Chaikin : MonoBehaviour
{
    #region Public Fields
    
    [Header("Chaikin")]
    public GameObject chaikinPrefab;
    public GameObject chaikinParent;
    [SerializeField]
    private bool loop;
    public bool Loop
    {
        get => loop;
        set
        {
            if (loop == value) return;
            
            loop = value;
            if (Application.isPlaying)
            {
                UpdateLines();
            }
        }
    }

    [SerializeField]
    [Range(0,5)]
    private int iterations = 3;
    public int Iterations
    {
        get => iterations;
        set
        {
            if (iterations == value) return;
            
            iterations = value;
            if (Application.isPlaying)
            {
                UpdateLines();
            }
        }
    }
    
    [SerializeField]
    [Range(0,1)]
    private float u;
    public float U
    {
        get => u;
        set
        {
            if (Math.Abs(u - value) < 0.001) return;
            
            u = value;
            if (Application.isPlaying)
            {
                UpdateLines();
            }
        }
    }
    
    [SerializeField]
    [Range(0,1)]
    private float v;
    public float V
    {
        get => v;
        set
        {
            if (Math.Abs(v - value) < 0.001) return;
            
            v = value;
            if (Application.isPlaying)
            {
                UpdateLines();
            }
        }
    }
    
    public static Chaikin Instance;
    
    #endregion
    
    #region Private Fields
    
    private List<GameObject> chaikinGO = new ();
    private List<Point> chaikinPoints = new();
    
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
    
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        StartCoroutine(DeferredUpdateLines());
    }
    
    private IEnumerator DeferredUpdateLines()
    {
        yield return new WaitForEndOfFrame();
        UpdateLines();
    }
    
    public List<Point> ChaikinSubdivision(List<Point> points)
    {
        var newPoints = new List<Point>();

        for (var i = 0; i < points.Count - 1; i++)
        {
            var p0 = points[i];
            var p1 = points[i + 1];

            var q = new Point(
                (float)(p0.X + u * (p1.X - p0.X)),
                (float)(p0.Y + u * (p1.Y - p0.Y)),
                (float)(p0.Z + u * (p1.Z - p0.Z)));
            var r = new Point(
                (float)(p0.X + v * (p1.X - p0.X)), 
                (float)(p0.Y + v * (p1.Y - p0.Y)),
                (float)(p0.Z + v * (p1.Z - p0.Z)));

            chaikinGO.Add(Instantiate(chaikinPrefab, q.GetVector(), Quaternion.identity, chaikinParent.transform));
            chaikinGO.Add(Instantiate(chaikinPrefab, r.GetVector(), Quaternion.identity, chaikinParent.transform));

            newPoints.Add(q);
            newPoints.Add(r);
            
            chaikinPoints.Add(q);
            chaikinPoints.Add(r);
        }

        if (loop && points.Count > 2)
        {
            var p0 = points[^1];
            var p1 = points[0];

            var q = new Point(
                (float)(p0.X + u * (p1.X - p0.X)),
                (float)(p0.Y + u * (p1.Y - p0.Y)),
                (float)(p0.Z + u * (p1.Z - p0.Z)));
            var r = new Point(
                (float)(p0.X + v * (p1.X - p0.X)), 
                (float)(p0.Y + v * (p1.Y - p0.Y)),
                (float)(p0.Z + v * (p1.Z - p0.Z)));
            
            chaikinGO.Add(Instantiate(chaikinPrefab, q.GetVector(), Quaternion.identity, chaikinParent.transform));
            chaikinGO.Add(Instantiate(chaikinPrefab, r.GetVector(), Quaternion.identity, chaikinParent.transform));

            newPoints.Add(q);
            newPoints.Add(r);
            
            chaikinPoints.Add(q);
            chaikinPoints.Add(r);
        }

        return newPoints;
    }

    public void UpdateLines()
    {
        // Delete lines
        foreach (var l in GameManager.Instance.linesGO)
        {
            Destroy(l);
        }
        
        // Delete chaikin points
        foreach(var p in chaikinGO)
        {
            Destroy(p);
        }
        
        GameManager.Instance.linesGO.Clear();
        chaikinGO.Clear();
        GameManager.Instance.allLines.Clear();
        chaikinPoints.Clear();

        if (GameManager.Instance.allPoints.Count <= 1) return;

        var subdividedPoints = new List<Point>(GameManager.Instance.allPoints);

        for (var i = 0; i < iterations; i++)
        {
            subdividedPoints = ChaikinSubdivision(subdividedPoints);
        }

        var currentPoint = subdividedPoints[0];

        for (var i = 1; i < subdividedPoints.Count; i++)
        {
            GameManager.Instance.DrawLine(GameManager.Instance.lineParent, currentPoint, subdividedPoints[i]);
            currentPoint = subdividedPoints[i];
        }

        if (loop && subdividedPoints.Count > 2)
        {
            GameManager.Instance.DrawLine(GameManager.Instance.lineParent, subdividedPoints.LastOrDefault(), subdividedPoints.FirstOrDefault());
        }
    }

    public void Delete()
    {
        // Delete chaikin points
        foreach(var p in chaikinGO)
        {
            Destroy(p);
        }
        
        chaikinGO.Clear();
        chaikinPoints.Clear();
    }
}
