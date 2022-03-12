using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static ProjectUtil;


public class ShapeInterpolator : MonoBehaviour
{
    // This function will get all the cross sections and will expand all cross sections
    // such that all the cross sections will be aligned and have the same number of vertices
    // This makes interpolation super easy
    public static ShapeData[] ExpandShapes(ShapeData[] shapes)
    {
        var toBeProcessed = new Stack<int>();
        var processed = new bool[shapes.Length];

        // find the shape with the max numPoints
        var maxIndex = 0;
        for (int i = 1; i < shapes.Length; i++)
        {
            var numPoints = shapes[i].GetNumPoints();
            if (numPoints > shapes[maxIndex].GetNumPoints())
            {
                maxIndex = i;
            }
        }

        // We will start expandsing shapes starting with the neighbors of the max numPoints shape
        // add the shapes that changed to the stack and change their neighbors aswell
        toBeProcessed.Push(maxIndex);

        while (toBeProcessed.Count > 0)
        {
            var index = toBeProcessed.Pop();

            var nextIndex = index + 1;
            var prevIndex = index - 1;

            if (nextIndex < shapes.Length && !processed[nextIndex])
            {
                AlignAndExpandShape(shapes, index, nextIndex); // process it
                toBeProcessed.Push(nextIndex); // add to the stack to process its neighbors
            }

            if (prevIndex >= 0 && !processed[prevIndex])
            {
                AlignAndExpandShape(shapes, index, prevIndex);
                toBeProcessed.Push(prevIndex);
            }

            processed[index] = true;
        }

        return shapes;
    }

    // We needed to do this for both the prev index and the next index, didnt' want to write it twice so made it a method
    private static void AlignAndExpandShape(ShapeData[] shapes, int index1, int index2)
    {
        // out of bounds
        if (index2 >= shapes.Length || index2 < 0)
        {
            return;
        }

        var newShape = AlignShape(shapes[index1].Get2DPoints(), shapes[index2].Get2DPoints());
        newShape = ExpandShape(shapes[index1].Get2DPoints(), newShape);
        shapes[index2].UpdateShape(newShape);
    }

    // Reference Shape has more vertices
    // This functions gets 2 polygons, referenceShape and originalShape
    // expands the originalShape by adding new vertices.
    // The location of the newly added vertices respects the relative distances on the referenceShape 
    private static Vector2[] ExpandShape(Vector2[] referenceShape, Vector2[] originalShape)
    {
        var expandedShape = new Vector2[referenceShape.Length];
        var skip = Mathf.CeilToInt((float)(referenceShape.Length) / originalShape.Length);
        if ((originalShape.Length - 1) * skip >= referenceShape.Length)
        {
            skip--;
        }

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

                // if (referenceShape.Length == 9)
                // {
                //     Debug.Log($"rs: {referenceShape.Length}, os:{originalShape.Length}, skip: {skip}, i: {i}, i1: {anchor1Index}, i2: {anchor2Index}");
                // }

                // Interpolate between the anchors to create new vertices
                var t = InverseLerpOnPolygon(referenceShape, anchor1Index * skip, Mathf.Min(anchor2Index * skip, referenceShape.Length - 1), i);
                var newVertex = Vector2.Lerp(originalShape[anchor1Index], originalShape[anchor2Index], t);

                expandedShape[i] = newVertex;
            }
        }

        return expandedShape;
    }

    // we know that 
    private static int[] DivideIntoKPieces(int[] a, int k)
    {

        return new int[0];
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

        for (int i = 0; i < numVertices; i++)
        {
            var x = Mathf.Lerp(firstShape.Get2DPoints()[i].x, secondShape.Get2DPoints()[i].x, t);
            var y = Mathf.Lerp(firstShape.Get2DPoints()[i].y, secondShape.Get2DPoints()[i].y, t);
            vertices[i] = new Vector2(x, y);
        }

        return new ShapeData(vertices, firstShape.GetPath(), tPath);
    }
}