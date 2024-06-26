using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using DelaunayVoronoi;

public class GameManager : MonoBehaviour
{
    #region Public Fields
    
    [Header("Points")]
    public GameObject pointPrefab;
    public GameObject pointParent;
    
    [Header("Lines")]
    public GameObject lineParent;
    
    [Header("Miscellaneous")]
    public Button btnBackground;
    
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
            Debug.Log("Set loop");
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
            Debug.Log("Set iterations");
            if (iterations == value) return;
            
            iterations = value;
            if (Application.isPlaying)
            {
                UpdateLines();
            }
        }
    }
    
    #endregion
    
    #region Private Fields

    private readonly List<GameObject> pointsGO = new ();
    private readonly List<GameObject> linesGO = new ();
    
    private readonly List<Point> allPoints = new ();
    private readonly List<LineRenderer> allLines = new ();

    private List<GameObject> chaikinGO = new ();
    private List<Point> chaikinPoints = new();

    #endregion

    private void Start()
    {
        btnBackground.onClick.AddListener(() => { });
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetButton("Fire1"))
        {
            var screenPosition = Input.mousePosition;
            var worldPosition = Camera.main!.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));

            var index = -1;
            var minDistance = 0f;
            
            for (var i = 0; i < pointsGO.Count; i++)
            {
                var currentDistance = Vector3.Distance(pointsGO[i].transform.position, worldPosition);
                if (minDistance == 0) minDistance = currentDistance;

                if (!(currentDistance <= minDistance)) continue;
                
                minDistance = currentDistance;
                index = i;
            }

            if (index != -1)
            {
                pointsGO[index].transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
                allPoints[index] = new Point(worldPosition.x, worldPosition.y);
            }
            
            UpdateLines();
            //UpdateRealtime();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            // Create point
            var screenPosition = Input.mousePosition;
            var worldPosition = Camera.main!.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
            pointsGO.Add(CreatePoint(worldPosition));
            allPoints.Add(new Point(worldPosition.x, worldPosition.y));
 
            UpdateLines();
            //UpdateRealtime();
        }

        if (Input.GetMouseButtonDown(1))
        {
            // Remove closest point
            var screenPosition = Input.mousePosition;
            var worldPosition = Camera.main!.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));

            var indexToRemove = -1;
            var minDistance = 0f;
            
            for (var i = 0; i < pointsGO.Count; i++)
            {
                var currentDistance = Vector3.Distance(pointsGO[i].transform.position, worldPosition);
                if (minDistance == 0) minDistance = currentDistance;

                if (!(currentDistance <= minDistance)) continue;
                
                minDistance = currentDistance;
                indexToRemove = i;
            }

            if (indexToRemove != -1)
            {
                Destroy(pointsGO[indexToRemove]);
                pointsGO.RemoveAt(indexToRemove);
                allPoints.RemoveAt(indexToRemove);
            }

            UpdateLines();
            //UpdateRealtime();
        }

        if (!Input.GetKeyDown(KeyCode.Delete)) return;
        
        // Delete points
        foreach(var p in pointsGO)
        {
            Destroy(p);
        }
        
        // Delete chaikin points
        foreach(var p in chaikinGO)
        {
            Destroy(p);
        }
        
        // Delete lines
        foreach(var l in linesGO)
        {
            Destroy(l);
        }
        
        pointsGO.Clear();
        linesGO.Clear();
        chaikinGO.Clear();
        allPoints.Clear();
        allLines.Clear();
        chaikinPoints.Clear();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)  // Avoid updating when the game isn't running
        {
            return;
        }

        StartCoroutine(DeferredUpdateLines());
    }
    
    private IEnumerator DeferredUpdateLines()
    {
        yield return new WaitForEndOfFrame();  // Wait until it's safe to update
        UpdateLines();
    }
    
    private void ClearChildren(GameObject parent)
    {
        for (var i = 0; i < parent.transform.childCount; i++)
        {
            Destroy(parent.transform.GetChild(i).gameObject);
        }
    }

    private void DrawLine(GameObject parent, Point start, Point end)
    {
        var lineObject = new GameObject("Line");
        var line = lineObject.AddComponent<LineRenderer>();

        line.material = new Material(Shader.Find("Sprites/Default"));

        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.positionCount = 2;
        line.useWorldSpace = true;
        
        line.startColor = Color.red;
        line.endColor = Color.red;

        var startVec = new Vector3(start.X, start.Y, 0);
        var endVec = new Vector3(end.X, end.Y, 0);
        line.SetPosition(0, startVec);
        line.SetPosition(1, endVec);

        lineObject.transform.parent = parent.transform;
        linesGO.Add(lineObject);
    }

    private List<Point> ChaikinSubdivision(List<Point> points)
    {
        var newPoints = new List<Point>();

        for (var i = 0; i < points.Count - 1; i++)
        {
            var p0 = points[i];
            var p1 = points[i + 1];

            var q = new Point((float)(p0.X + 0.25 * (p1.X - p0.X)), (float)(p0.Y + 0.25 * (p1.Y - p0.Y)));
            var r = new Point((float)(p0.X + 0.75 * (p1.X - p0.X)), (float)(p0.Y + 0.75 * (p1.Y - p0.Y)));

            chaikinGO.Add(Instantiate(chaikinPrefab, q.GetVector(), Quaternion.identity, chaikinParent.transform));
            chaikinGO.Add(Instantiate(chaikinPrefab, r.GetVector(), Quaternion.identity, chaikinParent.transform));

            newPoints.Add(q);
            newPoints.Add(r);
            
            chaikinPoints.Add(q);
            chaikinPoints.Add(r);
        }

        // Optionally close the loop if necessary
        if (loop && points.Count > 2)
        {
            var p0 = points[^1];
            var p1 = points[0];

            var q = new Point((float)(p0.X + 0.25 * (p1.X - p0.X)), (float)(p0.Y + 0.25 * (p1.Y - p0.Y)));
            var r = new Point((float)(p0.X + 0.75 * (p1.X - p0.X)), (float)(p0.Y + 0.75 * (p1.Y - p0.Y)));
            
            chaikinGO.Add(Instantiate(chaikinPrefab, q.GetVector(), Quaternion.identity, chaikinParent.transform));
            chaikinGO.Add(Instantiate(chaikinPrefab, r.GetVector(), Quaternion.identity, chaikinParent.transform));

            newPoints.Add(q);
            newPoints.Add(r);
            
            chaikinPoints.Add(q);
            chaikinPoints.Add(r);
        }

        return newPoints;
    }

    private void UpdateLines()
    {
        // Delete lines
        foreach (var l in linesGO)
        {
            Destroy(l);
        }
        
        // Delete chaikin points
        foreach(var p in chaikinGO)
        {
            Destroy(p);
        }
        
        linesGO.Clear();
        chaikinGO.Clear();
        allLines.Clear();
        chaikinPoints.Clear();

        if (allPoints.Count <= 1) return;

        var subdividedPoints = new List<Point>(allPoints);

        // Apply Chaikin subdivision for a number of iterations
        for (var i = 0; i < iterations; i++)
        {
            subdividedPoints = ChaikinSubdivision(subdividedPoints);
        }

        var currentPoint = subdividedPoints[0];

        for (var i = 1; i < subdividedPoints.Count; i++)
        {
            DrawLine(lineParent, currentPoint, subdividedPoints[i]);
            currentPoint = subdividedPoints[i];
        }

        if (loop && subdividedPoints.Count > 2)
        {
            DrawLine(lineParent, subdividedPoints.LastOrDefault(), subdividedPoints.FirstOrDefault());
        }
    }

    private GameObject CreatePoint(Vector3 position)
    {
        var zTo0Position = new Vector3(position.x, position.y, 0f);
        var point = Instantiate(pointPrefab, zTo0Position, Quaternion.identity, pointParent.transform);
        return point;
    }
}
