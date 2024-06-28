using System.Collections.Generic;
using UnityEngine;
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
    
    public static GameManager Instance;

    public bool coons;
    
    #endregion
    
    #region Private Fields

    internal List<GameObject> pointsGO = new ();
    internal List<GameObject> linesGO = new ();

    internal readonly List<Point> allPoints = new ();
    internal readonly List<LineRenderer> allLines = new ();

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

    private void Update()
    {
        if (coons) return;
        
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
            
            Chaikin.Instance.UpdateLines();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            // Create point
            var screenPosition = Input.mousePosition;
            var worldPosition = Camera.main!.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
            pointsGO.Add(CreatePoint(worldPosition));
            allPoints.Add(new Point(worldPosition.x, worldPosition.y));
            
            Chaikin.Instance.UpdateLines();
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
            
            Chaikin.Instance.UpdateLines();
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            // Delete points
            foreach(var p in pointsGO)
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
            allPoints.Clear();
            allLines.Clear();
        }
    }

    public GameObject CreatePoint(Vector3 position)
    {
        var zTo0Position = new Vector3(position.x, position.y, 0f);
        var point = Instantiate(pointPrefab, zTo0Position, Quaternion.identity, pointParent.transform);
        return point;
    }
    
    public void DrawLine(GameObject parent, Point start, Point end)
    {
        var lineObject = new GameObject("Line");
        var line = lineObject.AddComponent<LineRenderer>();

        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.red;
        line.endColor = Color.red;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.positionCount = 2;
        line.useWorldSpace = true;
        lineObject.transform.parent = parent.transform;

        var startVec = new Vector3(start.X, start.Y, start.Z);
        var endVec = new Vector3(end.X, end.Y, end.Z);
        
        line.SetPosition(0, startVec);
        line.SetPosition(1, endVec);
        
        linesGO.Add(lineObject);
    }
}
