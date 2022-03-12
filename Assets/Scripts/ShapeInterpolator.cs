using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using static ProjectUtil;
using System.Linq;


public class ShapeInterpolator : MonoBehaviour
{
    public static Vector2[][] FixShapes(Vector2[][] shapes)
    {
        // find the shape with max numVertices
        // convert its neighbors using pairshapes
        // repeat
        var newShape = ExpandShape(shapes[0], shapes[1]);
        shapes[1] = newShape;

        return shapes;
    }

    // Reference Shape has more vertices
    public static Vector2[] ExpandShape(Vector2[] modelShape, Vector2[] originalShape)
    {
        var expandedShape = new Vector2[modelShape.Length];
        var skip = Mathf.FloorToInt((float)modelShape.Length / originalShape.Length);

        // Now we will fill in the expandedShape
        for (var i = 0; i < expandedShape.Length; i++)
        {
            // This means that this is an index of an anchor point so we can keep it as is
            if (i % skip == 0 && i / skip < originalShape.Length)
            {
                expandedShape[i] = originalShape[i / skip];
            }
            // need to create a vertex
            else
            {
                var anchor1Index = Mathf.Min(i / skip, originalShape.Length - 1); // the anchor point before this vertex
                var anchor2Index = (Mathf.Min(i / skip + 1, originalShape.Length) % originalShape.Length); // the anchor point after this vertex

                var t = InverseLerp(modelShape[anchor1Index * skip], modelShape[anchor2Index * skip], modelShape[i]);
                var newVertex = Vector2.Lerp(originalShape[anchor1Index], originalShape[anchor2Index], t);

                Debug.Log($"i:{i}, anchor1: {anchor1Index}, anchor2: {anchor2Index}, t: {t}");

                expandedShape[i] = newVertex;
            }
        }

        return expandedShape;
    }

    // for (var i = 0; i < expandedShape.Length; i++)
    //     {
    //         // This means that this is an index of an anchor point so we can keep it as is
    //         if (i % skip == 0 && i / skip < originalShape.Length)
    //         {
    //             expandedShape[i] = originalShape[i / skip];
    //         }
    //         // need to create a vertex
    //         else
    //         {
    //             var anchor1Index = (i / skip); // the anchor point before this vertex
    //             var anchor2Index = ((i / skip + 1) % originalShape.Length); // the anchor point after this vertex
    //             Debug.Log($"i:{i}, anchor1: {anchor1Index}, anchor2: {anchor2Index}");

    //             var t = InverseLerp(modelShape[anchor1Index * skip], modelShape[anchor2Index * skip], modelShape[i]);
    //             var newVertex = Vector2.Lerp(originalShape[anchor1Index], originalShape[anchor2Index], t);

    //             expandedShape[i] = newVertex;
    //         }
    //     }

    // Both shapes are the same sizes right now, we will jsut align them
    private static Vector2[] AlignShapes(Vector2[] shape1, Vector2[] shape2)
    {
        // find the closest point to the first vertex of shape 1 in shape2, that gives the offset
        var offset = GetClosestPointIndex(shape2, shape1[0]);
        var alignedShape = new Vector2[shape1.Length];

        // shift the vertices using the offset
        for (var i = 0; i < shape2.Length; i++)
        {
            var newIndex = (i + offset) % shape1.Length;
            alignedShape[newIndex] = shape2[i];
        }

        return alignedShape;
    }

    // Returns the vertices of the shape between polygon1 and polygon2 at time t
    public static Vector2[] MorphShape(Vector2[] firstShape, Vector2[] secondShape, float t)
    {
        int numVertices = firstShape.Length;
        var vertices = new Vector2[numVertices];

        for (int i = 0; i < numVertices; i++)
        {
            var x = Mathf.Lerp(firstShape[i].x, secondShape[i].x, t);
            var y = Mathf.Lerp(firstShape[i].y, secondShape[i].y, t);
            vertices[i] = new Vector2(x, y);
        }

        return vertices;
    }
}