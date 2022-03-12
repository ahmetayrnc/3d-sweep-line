using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using static ProjectUtil;


public class ShapeInterpolator : MonoBehaviour
{
    // This function will get all the cross sections and will expand all cross sections
    // such that all the cross sections will be aligned and have the same number of vertices
    // This makes interpolation super easy
    public static ShapeData[] ExpandShapes(ShapeData[] shapes)
    {
        var stack = new Stack<int>();
        var maxIndex = 0;
        for (int i = 0; i < shapes.Length; i++)
        {
            var numPoints = shapes[i].GetNumPoints();
            if (numPoints > shapes[maxIndex].GetNumPoints())
            {
                maxIndex = i;
            }
        }
        stack.Push(maxIndex);

        while (stack.Count > 0)
        {
            var index = stack.Pop();
            if (index + 1 < shapes.Length)
            {
                if (shapes[index].GetNumPoints() != shapes[index + 1].GetNumPoints())
                {
                    var nextShape = AlignShape(shapes[index].Get2DPoints(), shapes[index + 1].Get2DPoints());
                    nextShape = ExpandShape(shapes[index].Get2DPoints(), nextShape);
                    shapes[index + 1].UpdateShape(nextShape);
                    stack.Push(index + 1);
                }
            }

            if (index - 1 >= 0)
            {
                if (shapes[index].GetNumPoints() != shapes[index - 1].GetNumPoints())
                {
                    var nextShape = AlignShape(shapes[index].Get2DPoints(), shapes[index - 1].Get2DPoints());
                    nextShape = ExpandShape(shapes[index].Get2DPoints(), nextShape);
                    shapes[index - 1].UpdateShape(nextShape);
                    stack.Push(index - 1);
                }
            }
        }

        return shapes;
    }

    // Reference Shape has more vertices
    // This functions gets 2 polygons, referenceShape and originalShape
    // expands the originalShape by adding new vertices.
    // The location of the newly added vertices respects the relative distances on the referenceShape 
    private static Vector2[] ExpandShape(Vector2[] referenceShape, Vector2[] originalShape)
    {
        var expandedShape = new Vector2[referenceShape.Length];
        var skip = Mathf.CeilToInt((float)referenceShape.Length / originalShape.Length);

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
                // Find the anchor points for interpolation
                var anchor1Index = Mathf.Min(i / skip, originalShape.Length - 1); // the anchor point before this vertex
                var anchor2Index = (Mathf.Min(i / skip + 1, originalShape.Length) % originalShape.Length); // the anchor point after this vertex

                // Interpolate between the anchors to create new vertices
                var t = InverseLerpOnPolygon(referenceShape, anchor1Index * skip, anchor2Index * skip, i);
                var newVertex = Vector2.Lerp(originalShape[anchor1Index], originalShape[anchor2Index], t);

                expandedShape[i] = newVertex;
            }
        }

        return expandedShape;
    }

    // Both shapes are the same sizes right now, we will jsut align them
    private static Vector2[] AlignShape(Vector2[] referenceShape, Vector2[] originalShape)
    {
        // find the closest point to the first vertex of shape 1 in shape2, that gives the offset
        var offset = GetClosestPointIndex(originalShape, referenceShape[0]);
        var alignedShape = new Vector2[originalShape.Length];

        // shift the vertices using the offset
        for (var i = 0; i < alignedShape.Length; i++)
        {
            alignedShape[i] = originalShape[(i + offset) % originalShape.Length];
        }

        return alignedShape;
    }

    // Returns the vertices of the shape between polygon1 and polygon2 at time t
    // This function is super simple because we assume that the firstShape and the secondShape have the same number
    // of vertices. Additionally, we also assume that these shapes are aligned and we can just connect the vertices of them in order
    public static ShapeData MorphShape(ShapeData firstShape, ShapeData secondShape, float t, float tPath)
    {
        int numVertices = firstShape.GetNumPoints();
        var vertices = new Vector2[numVertices];

        Debug.Assert(firstShape.GetNumPoints() == secondShape.GetNumPoints(), $"{firstShape.GetNumPoints()} == {secondShape.GetNumPoints()}");

        for (int i = 0; i < numVertices; i++)
        {
            var x = Mathf.Lerp(firstShape.Get2DPoints()[i].x, secondShape.Get2DPoints()[i].x, t);
            var y = Mathf.Lerp(firstShape.Get2DPoints()[i].y, secondShape.Get2DPoints()[i].y, t);
            vertices[i] = new Vector2(x, y);
        }

        return new ShapeData(vertices, firstShape.GetPath(), tPath);
    }
}