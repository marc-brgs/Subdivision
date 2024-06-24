using System;
using System.Collections.Generic;
using UnityEngine;

internal class PolarAngleComparer : IComparer<Vector3>
{
    private readonly Vector3 referencePoint;

    public PolarAngleComparer(Vector3 referencePoint)
    {
        this.referencePoint = referencePoint;
    }

    public int Compare(Vector3 a, Vector3 b)
    {
        var angleA = Math.Atan2(a.y - referencePoint.y, a.x - referencePoint.x);
        var angleB = Math.Atan2(b.y - referencePoint.y, b.x - referencePoint.x);
        
        if (angleA < angleB)
        {
            return -1;
        }

        return angleA > angleB ? 1 : 0;
    }
}