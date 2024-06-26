using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DelaunayVoronoi
{
    public class Point
    {
        public float X { get; }
        public float Y { get; }
        public HashSet<Triangle> AdjacentTriangles { get; } = new HashSet<Triangle>();

        public Point(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Vector3 GetVector()
        {
            return new Vector3(X, Y, 0);
        }
    }
}