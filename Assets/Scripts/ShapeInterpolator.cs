using UnityEngine;
using static ProjectUtil;


public class ShapeInterpolator : MonoBehaviour
{
    // This function will get all the cross sections and will expand all cross sections
    // such that all the cross sections will be aligned and have the same number of vertices
    // This makes interpolation super easy
    public static Vector2[][] ExpandShapes(Vector2[][] shapes)
    {
        // find the shape with max numVertices
        // convert its neighbors using pairshapes
        // repeat
        Vector2[] newShape;
        newShape = AlignShape(shapes[0], shapes[1]);
        newShape = ExpandShape(shapes[0], newShape);
        shapes[1] = newShape;

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