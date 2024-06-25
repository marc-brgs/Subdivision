using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using DelaunayVoronoi;

public class GameManager : MonoBehaviour
{
    private LineRenderer lrPolygon;
    private LineRenderer lrWindow;

    private bool drawingPolygon;
    private bool drawingWindow;
    private int polygonIndex;
    private int windowIndex;
    private Color fillColor;
    
    public GameObject pointPrefab;
    public GameObject lineParent;
    private readonly List<GameObject> points = new ();
    private readonly List<Point> allPoints = new ();
    private readonly List<GameObject> allLines = new ();

    public LineRenderer lineRenderer;

    public Button btnBackground;

    private readonly bool[] realtime = { false, false, false, false, false };

    public bool loop;

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
            
            for (var i = 0; i < points.Count; i++)
            {
                var currentDistance = Vector3.Distance(points[i].transform.position, worldPosition);
                if (minDistance == 0) minDistance = currentDistance;

                if (!(currentDistance <= minDistance)) continue;
                
                minDistance = currentDistance;
                index = i;
            }

            if (index != -1)
            {
                points[index].transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
                allPoints[index] = new Point(worldPosition.x, worldPosition.y);
            }
            
            UpdateLines();
            UpdateRealtime();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            // Create point
            var screenPosition = Input.mousePosition;
            var worldPosition = Camera.main!.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
            points.Add(CreatePoint(worldPosition));
            allPoints.Add(new Point(worldPosition.x, worldPosition.y));
 
            UpdateLines();
            UpdateRealtime();
        }

        if (Input.GetMouseButtonDown(1))
        {
            // Remove closest point
            var screenPosition = Input.mousePosition;
            var worldPosition = Camera.main!.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));

            var indexToRemove = -1;
            var minDistance = 0f;
            
            for (var i = 0; i < points.Count; i++)
            {
                var currentDistance = Vector3.Distance(points[i].transform.position, worldPosition);
                if (minDistance == 0) minDistance = currentDistance;

                if (!(currentDistance <= minDistance)) continue;
                
                minDistance = currentDistance;
                indexToRemove = i;
            }

            if (indexToRemove != -1)
            {
                Destroy(points[indexToRemove]);
                points.RemoveAt(indexToRemove);
                allPoints.RemoveAt(indexToRemove);
            }

            UpdateLines();
            UpdateRealtime();
        }

        if (!Input.GetKeyDown(KeyCode.Delete)) return;
        
        // Delete points
        foreach(var p in points)
        {
            Destroy(p);
        }
        
        // Delete lines
        foreach(var l in allLines)
        {
            Destroy(l);
        }
        
        points.Clear();
        allPoints.Clear();
        allLines.Clear();
    }

    private List<Point> ChaikinSubdivision(List<Point> points)
    {
        List<Point> newPoints = new List<Point>();

        for (int i = 0; i < points.Count - 1; i++)
        {
            Point p0 = points[i];
            Point p1 = points[i + 1];

            Point q = new Point(p0.X + 0.25 * (p1.X - p0.X), p0.Y + 0.25 * (p1.Y - p0.Y));
            Point r = new Point(p0.X + 0.75 * (p1.X - p0.X), p0.Y + 0.75 * (p1.Y - p0.Y));

            newPoints.Add(q);
            newPoints.Add(r);
        }

        // Optionally close the loop if necessary
        if (loop && points.Count > 2)
        {
            Point p0 = points[points.Count - 1];
            Point p1 = points[0];

            Point q = new Point(p0.X + 0.25 * (p1.X - p0.X), p0.Y + 0.25 * (p1.Y - p0.Y));
            Point r = new Point(p0.X + 0.75 * (p1.X - p0.X), p0.Y + 0.75 * (p1.Y - p0.Y));

            newPoints.Add(q);
            newPoints.Add(r);
        }

        return newPoints;
    }

    private void UpdateLines()
    {
        // Delete lines
        foreach (var l in allLines)
        {
            Destroy(l);
        }
        allLines.Clear();

        if (allPoints.Count <= 1) return;

        List<Point> subdividedPoints = new List<Point>(allPoints);

        // Apply Chaikin subdivision for a number of iterations
        int iterations = 3; // You can adjust the number of iterations
        for (int i = 0; i < iterations; i++)
        {
            subdividedPoints = ChaikinSubdivision(subdividedPoints);
        }

        var currentPoint = subdividedPoints[0];

        for (int i = 1; i < subdividedPoints.Count; i++)
        {
            DrawLine(lineParent, currentPoint, subdividedPoints[i]);
            currentPoint = subdividedPoints[i];
        }

        if (loop && subdividedPoints.Count > 2)
        {
            DrawLine(lineParent, subdividedPoints.LastOrDefault(), subdividedPoints.FirstOrDefault());
        }
    }

    private void UpdateRealtime()
    {
        if (realtime[0] == true)
        {
        }
        
        if (realtime[1] == true)
        {
        }
        
        if (realtime[3] == true)
        {
        }
        
        if (realtime[4] == true)
        {
        }
    }

    private GameObject CreatePoint(Vector3 position)
    {
        var zTo0Position = new Vector3(position.x, position.y, 0f);
        var point = Instantiate(pointPrefab, zTo0Position, Quaternion.identity);
        return point;
    }


    private bool IsClockwise(GameObject a, GameObject b, GameObject c)
    {
        var position = a.transform.position;
        Vector2 ab = b.transform.position - position;
        Vector2 ac = c.transform.position - position;
        return (ab.x * ac.y - ab.y * ac.x) <= 0;
    }

    private Vector3 ComputeCentroid(List<GameObject> inputPoints)
    {
        var centroid = inputPoints.Aggregate(Vector3.zero, (current, point) => current + point.transform.position);

        centroid /= inputPoints.Count;

        return centroid;
    }

    private List<Vector3> SortPointsByPolarAngle(List<GameObject> inputPoints, Vector3 centroid)
    {
        var sortedPoints = inputPoints
            .Select(p => p.transform.position)
            .OrderBy(p => Mathf.Atan2(p.y - centroid.y, p.x - centroid.x))
            .ToList();

        return sortedPoints;
    }

    private bool IsConvex(LinkedListNode<Vector3> point, Vector3 centroid, LinkedList<Vector3> convexHullLinkedList)
    {
        var prevNode = point.Previous ?? convexHullLinkedList.Last;
        var nextNode = point.Next ?? convexHullLinkedList.First;

        var prev = prevNode.Value;
        var next = nextNode.Value;

        var angle = Mathf.Atan2(point.Value.y - centroid.y, point.Value.x - centroid.x);
        var anglePrev = Mathf.Atan2(prev.y - centroid.y, prev.x - centroid.x);
        var angleNext = Mathf.Atan2(next.y - centroid.y, next.x - centroid.x);

        return anglePrev <= angle && angle <= angleNext;
    }

    private bool IsCounterClockwise(Vector3 a, Vector3 b, GameObject c)
    {
        Vector2 ab = b - a;
        Vector2 ac = c.transform.position - a;
        return (ab.x * ac.y - ab.y * ac.x) > 0;
    }
    private void ClearLines(GameObject parent)
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

        var startVec = new Vector3((float)start.X, (float)start.Y, 0);
        var endVec = new Vector3((float)end.X, (float)end.Y, 0);
        line.SetPosition(0, startVec);
        line.SetPosition(1, endVec);

        lineObject.transform.parent = parent.transform;
        allLines.Add(lineObject);
    }

    private Vector3[] SortPointsByPolarAngle(Vector3[] allPoints)
    {
        var minXIndex = 0;
        for (var i = 1; i < allPoints.Length; i++)
        {
            if (allPoints[i].x < allPoints[minXIndex].x)
            {
                minXIndex = i;
            }
        }

        (allPoints[0], allPoints[minXIndex]) = (allPoints[minXIndex], allPoints[0]);
        Array.Sort(allPoints, 1, allPoints.Length - 1, new PolarAngleComparer(allPoints[0]));

        return allPoints;
    }
}
