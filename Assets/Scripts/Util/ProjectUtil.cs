using System;
using System.Linq;
using UnityEngine;
using SVGMeshUnity;
using PathCreation;
using System.Text.RegularExpressions;

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

    public static SVGData SVGStringTOSVGData(String svgString)
    {
        var pattern = @"<path.*d=\""(.*?)\"".*\/>";
        var path = Regex.Match(svgString, pattern).Groups[1].Value;

        var svgData = new SVGData();
        svgData.Path(path);
        return svgData;
    }

    public static ShapeData SVGToShapeData(SVGData svg, float rotation, Vector2 scale, PathCreator pathCreator, float t)
    {
        var svgMesh = new SVGMesh();
        var mesh = svgMesh.FillMesh(svg);

        // convert to vector2
        var vertices = mesh.vertices.Select(v => (Vector2)v);

        // Scale the shape between -1 and 1
        var min_x = vertices.Select(v => v.x).Min();
        var min_y = vertices.Select(v => v.y).Min();
        var max_x = vertices.Select(v => v.x).Max();
        var max_y = vertices.Select(v => v.y).Max();

        vertices = vertices.Select(v =>
        {
            return new Vector2(
                2.0f * (v.x - min_x) / (max_x - min_x) - 1.0f,
                2.0f * (v.y - min_y) / (max_y - min_y) - 1.0f);
        }).ToArray();

        // rotation
        vertices = vertices.Select(v => (Vector2)(Quaternion.Euler(0, 0, rotation) * v));

        //scale
        vertices = vertices.Select(v => v * scale);

        // Make sure the shape is in clockwise order
        if ((vertices.ElementAt(1).x - vertices.ElementAt(0).x) * (vertices.ElementAt(1).y + vertices.ElementAt(0).y) < 0)
        {
            vertices = vertices.Reverse().ToArray();
        }

        return new ShapeData(vertices.ToArray(), pathCreator.path, t);
    }
}

