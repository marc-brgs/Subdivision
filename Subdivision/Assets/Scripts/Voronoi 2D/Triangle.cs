using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace DelaunayVoronoi
{
    public class Triangle
    {
        public Point[] Vertices { get; } = new Point[3];
        public Point CentreCirconscrit { get; private set; }
        public double RadiusSquared;

        public IEnumerable<Triangle> TrianglesWithSharedEdge
        {
            get
            {
                var neighbors = new HashSet<Triangle>();
                foreach (var vertex in Vertices)
                {
                    var trianglesWithSharedEdge = vertex.AdjacentTriangles.Where(o =>
                    {
                        return o != this && SharesEdgeWith(o);
                    });
                    neighbors.UnionWith(trianglesWithSharedEdge);
                }

                return neighbors;
            }
        }

        public Triangle(Point point1, Point point2, Point point3)
        {
            if (point1 == point2 || point1 == point3 || point2 == point3)
            {
                throw new ArgumentException("Création d'un triangle avec 3 points non distincts");
            }

            if (!IsCounterClockwise(point1, point2, point3))
            {
                Vertices[0] = point1;
                Vertices[1] = point3;
                Vertices[2] = point2;
            }
            else
            {
                Vertices[0] = point1;
                Vertices[1] = point2;
                Vertices[2] = point3;
            }

            Vertices[0].AdjacentTriangles.Add(this);
            Vertices[1].AdjacentTriangles.Add(this);
            Vertices[2].AdjacentTriangles.Add(this);
            UpdateCercleCirconscrit();
        }

        private void UpdateCercleCirconscrit()
        {
            // https://codefound.wordpress.com/2013/02/21/how-to-compute-a-circumcircle/#more-58
            Point p0 = Vertices[0];
            Point p1 = Vertices[1];
            Point p2 = Vertices[2];
            double dA = p0.X * p0.X + p0.Y * p0.Y;
            double dB = p1.X * p1.X + p1.Y * p1.Y;
            double dC = p2.X * p2.X + p2.Y * p2.Y;

            double aux1 = (dA * (p2.Y - p1.Y) + dB * (p0.Y - p2.Y) + dC * (p1.Y - p0.Y));
            double aux2 = -(dA * (p2.X - p1.X) + dB * (p0.X - p2.X) + dC * (p1.X - p0.X));
            double div = (2 * (p0.X * (p2.Y - p1.Y) + p1.X * (p0.Y - p2.Y) + p2.X * (p1.Y - p0.Y)));

            if (div == 0)
            {
                throw new DivideByZeroException(); // Points collinéaires
            }

            Point center = new Point(aux1 / div, aux2 / div);
            CentreCirconscrit = center;
            RadiusSquared = (center.X - p0.X) * (center.X - p0.X) + (center.Y - p0.Y) * (center.Y - p0.Y);
        }

        private bool IsCounterClockwise(Point point1, Point point2, Point point3)
        {
            var result = (point2.X - point1.X) * (point3.Y - point1.Y) -
                (point3.X - point1.X) * (point2.Y - point1.Y);
            return result > 0;
        }

        public bool SharesEdgeWith(Triangle triangle)
        {
            var sharedVertices = Vertices.Where(o => triangle.Vertices.Contains(o)).Count();
            return sharedVertices == 2;
        }

        public bool IsPointInsideCircleCirconscrit(Point point)
        {
            // sqrt(x² + y²)
            double distanceSquared = (point.X - CentreCirconscrit.X) * (point.X - CentreCirconscrit.X) +
                (point.Y - CentreCirconscrit.Y) * (point.Y - CentreCirconscrit.Y);
            return distanceSquared < RadiusSquared;
        }
    }
}