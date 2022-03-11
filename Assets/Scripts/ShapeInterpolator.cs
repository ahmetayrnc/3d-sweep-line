using UnityEngine;
using System.Linq;
using static ProjectUtil;

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
    public static Vector2[] ExpandShape(Vector2[] referenceShape, Vector2[] originalShape)
    {
        var fixedShape = new Vector2[referenceShape.Length];
        var skip = referenceShape.Length / originalShape.Length + 1;

        for (var i = 0; i < originalShape.Length; i++)
        {
            var newIndex = i * skip;
            fixedShape[newIndex] = originalShape[i];
        }

        for (var i = 0; i < referenceShape.Length; i++)
        {
            // original vertex
            if (i % skip == 0)
            {
                fixedShape[i] = originalShape[i / skip];
            }
            // need to create a vertex
            else
            {
                var t = InverseLerp(referenceShape[i - 1], referenceShape[(i + 1) % referenceShape.Length], referenceShape[i]);
                var newVertex = Vector2.Lerp(originalShape[i / skip], originalShape[(i / skip + 1) % originalShape.Length], t);
                fixedShape[i] = newVertex;
            }
        }

        return fixedShape;
    }

    // Both shapes are the same sizes right now, we will jsut align them
    public static Vector2[] AlignShapes(Vector2[] shape1, Vector2[] shape2)
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