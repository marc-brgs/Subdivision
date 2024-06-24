using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DelaunayVoronoi
{
    public class Voronoi
    {
        // On relie le centre des cercles circonscris de chaque triangle
        public List<Edge> GenerateEdgesFromDelaunay(IEnumerable<Triangle> triangulation)
        {
            List<Edge> voronoiEdges = new List<Edge>();
            foreach (Triangle triangle in triangulation)
            {
                foreach (Triangle neighbor in triangle.TrianglesWithSharedEdge)
                {
                    Edge edge = new Edge(triangle.CentreCirconscrit, neighbor.CentreCirconscrit);
                    voronoiEdges.Add(edge);
                }
            }

            return voronoiEdges;
        }
    }
}