using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using DelaunayVoronoi;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    [SerializeField] private GameObject polygon;
    [SerializeField] private GameObject window;

    private LineRenderer lrPolygon;
    private LineRenderer lrWindow;

    private bool drawingPolygon;
    private bool drawingWindow;
    private int polygonIndex;
    private int windowIndex;

    [SerializeField] private GameObject textPolygon;
    [SerializeField] private GameObject textWindow;
    
    [SerializeField] private Image img;
    private Color fillColor;

    // Voronoi
    [SerializeField] private GameObject pointPrefab;
    private List<GameObject> points = new List<GameObject>();

    [SerializeField] private LineRenderer lineRenderer;

    [SerializeField] private GameObject delaunayLines;
    [SerializeField] private GameObject voronoiLines;

    public enum ConvexHullMethod { Jarvis, GrahamScan };
    public enum TriangulationMethod { Delaunay, Voronoi};

    [SerializeField] Button btnBackground;
    [SerializeField] Button btnJarvis;
    [SerializeField] Button btnGraham;
    [SerializeField] Button btnIncremental;
    [SerializeField] Button btnDelaunay;
    [SerializeField] Button btnVoronoi;

    private bool[] realtime = new bool[] { false, false, false, false, false };
    //private int realtime = 0;

    // Start is called before the first frame update
    void Start()
    {
        btnBackground.onClick.AddListener(() => { });

        // Buttons listeners
        btnJarvis.onClick.AddListener(() =>
        {
            Destroy(points[points.Count-1]);
            points.RemoveAt(points.Count-1);
            clearConvexHull();

            realtime[0] = !realtime[0];
            if (!realtime[0]) return;

            updateRealtime();
        });

        btnGraham.onClick.AddListener(() =>
        {
            Destroy(points[points.Count - 1]);
            points.RemoveAt(points.Count - 1);
            clearConvexHull();

            realtime[1] = !realtime[1];
            if (!realtime[1]) return;

            updateRealtime();
        });

        btnIncremental.onClick.AddListener(() =>
        {
            Destroy(points[points.Count - 1]);
            points.RemoveAt(points.Count - 1);

            realtime[2] = !realtime[2];
            if (!realtime[2]) return;

            updateRealtime();
        });

        btnDelaunay.onClick.AddListener(() =>
        {
            Destroy(points[points.Count - 1]);
            points.RemoveAt(points.Count - 1);
            clearLines(delaunayLines);

            realtime[3] = !realtime[3];
            if (!realtime[3]) return;

            updateRealtime();
        });

        btnVoronoi.onClick.AddListener(() =>
        {
            Destroy(points[points.Count - 1]);
            points.RemoveAt(points.Count - 1);
            clearLines(voronoiLines);

            realtime[4] = !realtime[4];
            if (!realtime[4]) return;

            updateRealtime();
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetButton("Fire1"))
        {
            // Move closest point
            Vector3 screenPosition = Input.mousePosition;
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));

            int index = -1;
            float minDistance = 0;
            for (int i = 0; i < points.Count; i++)
            {
                float currentDistance = Vector3.Distance(points[i].transform.position, worldPosition);
                if (minDistance == 0) minDistance = currentDistance;

                if (currentDistance <= minDistance)
                {
                    minDistance = currentDistance;
                    index = i;
                }
            }

            if(index != -1)
            {
                points[index].transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
            }
            

            updateRealtime();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            // Create point
            Vector3 screenPosition = Input.mousePosition;
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
            points.Add(CreatePoint(worldPosition));

            updateRealtime();
        }

        if (Input.GetMouseButtonDown(1))
        {
            // Remove closest point
            Vector3 screenPosition = Input.mousePosition;
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));

            int indexToRemove = -1;
            float minDistance = 0;
            for (int i = 0; i < points.Count; i++)
            {
                float currentDistance = Vector3.Distance(points[i].transform.position, worldPosition);
                if (minDistance == 0) minDistance = currentDistance;

                if (currentDistance <= minDistance)
                {
                    minDistance = currentDistance;
                    indexToRemove = i;
                }
            }

            if (indexToRemove != -1)
            {
                Destroy(points[indexToRemove]);
                points.RemoveAt(indexToRemove);
            }

            updateRealtime();
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            // Delete points
            foreach(GameObject p in points)
            {
                Destroy(p);
            }
            points.Clear();
            clearLines(delaunayLines);
            clearLines(voronoiLines);
        }
    }

    private void updateRealtime()
    {
        if (realtime[0] == true)
        {
            ComputeConvexHull(points, ConvexHullMethod.Jarvis);
        }
        if (realtime[1] == true)
        {
            ComputeConvexHull(points, ConvexHullMethod.GrahamScan);
        }
        if (realtime[3] == true)
        {
            clearLines(delaunayLines);
            GenerateVoronoiDiagram(TriangulationMethod.Delaunay);
        }
        if (realtime[4] == true)
        {
            clearLines(voronoiLines);
            GenerateVoronoiDiagram(TriangulationMethod.Voronoi);
        }
    }

    private void clearConvexHull()
    {
        lineRenderer.SetPositions(Array.Empty<Vector3>());
        lineRenderer.positionCount = 0;
    }

    private GameObject CreatePoint(Vector3 position)
    {
        Vector3 zTo0Position = new Vector3(position.x, position.y, 0f);
        GameObject point = Instantiate(pointPrefab, zTo0Position, Quaternion.identity);
        return point;
    }


    public void ComputeConvexHull(List<GameObject> points, ConvexHullMethod method)
    {
        List<Vector3> convexHullPoints = new List<Vector3>();
        if(method == ConvexHullMethod.Jarvis)
            convexHullPoints = JarvisMarch(points);
        else if(method == ConvexHullMethod.GrahamScan) {
            convexHullPoints = GrahamScan2(points);
        }

        // Créez un LineRenderer pour afficher l'enveloppe convexe
        lineRenderer.positionCount = 0;

        // Remplissez le LineRenderer avec les points de l'enveloppe convexe
        for (int i = 0; i < convexHullPoints.Count; i++)
        {
            lineRenderer.positionCount = i+1;
            lineRenderer.SetPosition(i, convexHullPoints[i]);
        }
    }

    // Algorithme de Jarvis pour calculer l'enveloppe convexe
    private List<Vector3> JarvisMarch(List<GameObject> inputPoints)
    {
        List<Vector3> convexHull = new List<Vector3>();

        if (inputPoints.Count < 3)
            return convexHull;

        // Trouvez le point le plus à gauche comme point de départ
        GameObject startPoint = inputPoints[0];
        foreach (GameObject point in inputPoints)
        {
            if (point.transform.position.x < startPoint.transform.position.x)
            {
                startPoint = point;
            }
        }

        // Ajoutez le point de départ à l'enveloppe convexe
        convexHull.Add(startPoint.transform.position);

        GameObject currentPoint = startPoint;
        do
        {
            GameObject nextPoint = null;
            foreach (GameObject point in inputPoints)
            {
                if (point == currentPoint)
                    continue;
                if (nextPoint == null || IsClockwise(currentPoint, point, nextPoint))
                {
                    nextPoint = point;
                }
            }

            if (nextPoint != startPoint)
            {
                convexHull.Add(nextPoint.transform.position);
            }

            currentPoint = nextPoint;
        }
        while (currentPoint != startPoint);

        return convexHull;
    }

    // Vérifie si les points sont disposés dans le sens des aiguilles d'une montre
    private bool IsClockwise(GameObject a, GameObject b, GameObject c)
    {
        Vector2 ab = b.transform.position - a.transform.position;
        Vector2 ac = c.transform.position - a.transform.position;
        return (ab.x * ac.y - ab.y * ac.x) <= 0;
    }

    private Vector3 ComputeCentroid(List<GameObject> inputPoints)
    {
        Vector3 centroid = Vector3.zero;

        foreach (GameObject point in inputPoints)
        {
            centroid += point.transform.position;
        }

        centroid /= inputPoints.Count;

        return centroid;
    }

    private List<Vector3> SortPointsByPolarAngle(List<GameObject> inputPoints, Vector3 centroid)
    {
        List<Vector3> sortedPoints = inputPoints
            .Select(p => p.transform.position)
            .OrderBy(p => Mathf.Atan2(p.y - centroid.y, p.x - centroid.x))
            .ToList();

        return sortedPoints;
    }

    private bool IsConvex(LinkedListNode<Vector3> point, Vector3 centroid, LinkedList<Vector3> convexHullLinkedList)
    {
        LinkedListNode<Vector3> prevNode = point.Previous ?? convexHullLinkedList.Last;
        LinkedListNode<Vector3> nextNode = point.Next ?? convexHullLinkedList.First;

        Vector3 prev = prevNode.Value;
        Vector3 next = nextNode.Value;

        // Calculez les angles orientés
        float angle = Mathf.Atan2(point.Value.y - centroid.y, point.Value.x - centroid.x);
        float anglePrev = Mathf.Atan2(prev.y - centroid.y, prev.x - centroid.x);
        float angleNext = Mathf.Atan2(next.y - centroid.y, next.x - centroid.x);

        return anglePrev <= angle && angle <= angleNext;
    }

    // Algorithme du Graham Scan pour calculer l'enveloppe convexe.
    private List<Vector3> GrahamScan(List<GameObject> inputPoints)
    {
        List<Vector3> convexHull = new List<Vector3>();

        if (inputPoints.Count < 3)
            return convexHull;

        Vector3 barycentre = ComputeCentroid(inputPoints);
        List<Vector3> sortedPoints = SortPointsByPolarAngle(inputPoints, barycentre);

        // Suppression des points non convexes
        LinkedList<Vector3> convexHullLinkedList = new LinkedList<Vector3>(sortedPoints);

        LinkedListNode<Vector3> pivot = convexHullLinkedList.First;
        LinkedListNode<Vector3> start = pivot;

        bool advance = true;

        do
        {
            if (IsConvex(pivot, barycentre, convexHullLinkedList))
            {
                pivot = pivot.Next ?? convexHullLinkedList.First;
                advance = true;
            }
            else
            {
                start = pivot.Previous ?? convexHullLinkedList.Last;
                convexHullLinkedList.Remove(pivot);
                pivot = start;
                advance = false;
            }
        } while (pivot != start || !advance);

        convexHull.Clear();
        
        foreach (Vector3 point in convexHullLinkedList)
        {
            convexHull.Add(point);
        }

        return convexHull;
    }

    // Vérifie si les points sont disposés dans le sens trigonométrique (anti-horaire)
    private bool IsCounterClockwise(Vector3 a, Vector3 b, GameObject c)
    {
        Vector2 ab = b - a;
        Vector2 ac = c.transform.position - a;
        return (ab.x * ac.y - ab.y * ac.x) > 0;
    }


    // VORONOI ET DELAUNAY

    void clearLines(GameObject parent)
    {
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Destroy(parent.transform.GetChild(i).gameObject);
        }
    }

    void GenerateVoronoiDiagram(TriangulationMethod method)
    {
        int numberOfPoints = 100;
        double maxX = 20;
        double maxY = 20;


        //var convertedPoints = ConvertGameObjectsToPoints(points);

        // Générer les points
        var delaunay = new DelaunayTriangulator();
        //var points = delaunay.GeneratePoints(numberOfPoints, maxX, maxY);
        var convertedPoints = delaunay.ConvertAndInitialize(points);

        // Créer la triangulation de Delaunay
        HashSet<Triangle> triangles = delaunay.BowyerWatson(convertedPoints);

        // Générer le diagramme de Voronoi
        var voronoi = new Voronoi();
        List<DelaunayVoronoi.Edge> voronoiEdges = voronoi.GenerateEdgesFromDelaunay(triangles);

        if(method == TriangulationMethod.Delaunay)
        {
            DrawDelaunayTriangles(triangles);
        }
        else if(method == TriangulationMethod.Voronoi)
        {
            DrawVoronoiEdges(voronoiEdges);
        }
    }

    private void DrawDelaunayTriangles(IEnumerable<Triangle> triangles)
    {
        foreach (var triangle in triangles)
        {
            DrawLine(delaunayLines, triangle.Vertices[0], triangle.Vertices[1]);
            DrawLine(delaunayLines, triangle.Vertices[1], triangle.Vertices[2]);
            DrawLine(delaunayLines, triangle.Vertices[2], triangle.Vertices[0]);
        }
    }

    private void DrawVoronoiEdges(List<DelaunayVoronoi.Edge> edges)
    {
        foreach (var edge in edges)
        {
            DrawLine(voronoiLines, edge.Point1, edge.Point2);
        }
    }

    private void DrawLine(GameObject parent, Point start, Point end)
    {
        // Créer un nouveau GameObject pour chaque ligne
        GameObject lineObject = new GameObject("Line");
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        // Définir le matériau du LineRenderer
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        if(parent.gameObject.name == "VoronoiLines") {
            lineRenderer.material.color = new Color(0.3f, 0.8f, 0.9f);
        }

        // Configuration du LineRenderer
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        // Définir les positions de départ et de fin
        Vector3 startVec = new Vector3((float)start.X, (float)start.Y, 0);
        Vector3 endVec = new Vector3((float)end.X, (float)end.Y, 0);
        lineRenderer.SetPosition(0, startVec);
        lineRenderer.SetPosition(1, endVec);

        lineObject.transform.parent = parent.transform;
    }


    // OTHER

    private List<Vector3> GrahamScan2(List<GameObject> inputPoints)
    {
        return JarvisMarch(inputPoints);
    }

    /**
     * Tri les points par angle polaire
     */
    Vector3[] SortPointsByPolarAngle(Vector3[] points)
    {
        // Trouver le point le plus à gauche (avec le plus petit abscisse)
        int minXIndex = 0;
        for (int i = 1; i < points.Length; i++)
        {
            if (points[i].x < points[minXIndex].x)
            {
                minXIndex = i;
            }
        }

        // Déplacer le point le plus à gauche en tête de liste
        (points[0], points[minXIndex]) = (points[minXIndex], points[0]);

        // Trier les points restants par angle polaire croissant
        Array.Sort(points, 1, points.Length - 1, new PolarAngleComparer(points[0]));

        return points;
    }
}

/*public class Edge
{
    public Vector3 P1 { get; set; }
    public Vector3 P2 { get; set; }

    public Edge(Vector3 p1, Vector3 p2)
    {
        P1 = p1;
        P2 = p2;
    }
}*/