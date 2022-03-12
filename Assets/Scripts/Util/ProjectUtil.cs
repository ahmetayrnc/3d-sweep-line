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

    public static float InverseLerp(Vector2 a, Vector2 b, Vector2 value)
    {
        var AB = b - a;
        var AV = value - a;
        var result = Vector2.Dot(AV, AB) / Vector2.Dot(AB, AB);
        return result;
    }
}

