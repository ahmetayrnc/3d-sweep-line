using System;
using System.Linq;
using UnityEngine;


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

    public static int GetClosestPointIndex(Vector2[] points, Vector2 reference)
    {
        if (points.Length == 0)
        {
            return -1; //TODO
        }

        int minIndex = 0;
        var distances = points.Select(p => (p - reference).sqrMagnitude).ToArray();
        for (int i = 0; i < distances.Length; i++)
        {
            if (distances[i] < distances[minIndex])
            {
                minIndex = i;
            }
        }

        return minIndex;
    }

    public static float InverseLerpOnPolygon(Vector2[] polygon, int startIndex, int endIndex, int midIndex)
    {
        var diff1 = DistanceBetweenPointsOnPolygon(polygon, startIndex, midIndex);
        var diff2 = DistanceBetweenPointsOnPolygon(polygon, startIndex, endIndex);
        var result = (diff1 / diff2);
        return result;
    }

    public static float DistanceBetweenPointsOnPolygon(Vector2[] polygon, int index1, int index2)
    {
        var distance = 0f;
        var to = index2 == 0 ? polygon.Length : index2;

        for (var i = index1; i < to; i++)
        {
            var vertex1 = polygon[i];
            var vertex2 = polygon[(i + 1) % polygon.Length];
            distance += Vector2.Distance(vertex1, vertex2);
        }

        return distance;
    }
}

