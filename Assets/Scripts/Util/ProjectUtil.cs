using System;
using System.Linq;
using UnityEngine;
using MinByExtension;


public static class ProjectUtil
{
    public static T[] ConcatArrays<T>(params T[][] p)
    {
        var position = 0;
        var outputArray = new T[p.Sum(a => a.Length)];
        foreach (var curr in p)
        {
            Array.Copy(curr, 0, outputArray, position, curr.Length);
            position += curr.Length;
        }
        return outputArray;
    }

    private static Vector3 GetClosestPoint(Vector3[] points, Vector3 reference)
    {
        if (points.Length == 0)
        {
            return Vector3.zero; //TODO
        }

        var closestPoint = points.MinBy(p => (p - reference).sqrMagnitude);
        return closestPoint;
    }
}

