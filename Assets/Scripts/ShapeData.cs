using UnityEngine;
using PathCreation;
using System.Linq;
using Vector3Extension;

public class ShapeData
{
    private VertexPath path;
    private float t;
    private Vector2[] points;

    public ShapeData(Vector2[] points, VertexPath path, float t)
    {
        this.points = points;
        this.t = t;
        this.path = path;
    }

    public Vector2[] Get2DPoints()
    {
        return points;
    }

    // Uses the actual 3D coordinate system
    public Vector3[] Get3DPoints()
    {
        var points3D = points.Select(p => p.To3DPoint(path, t)).ToArray();
        return points3D;
    }

    public int GetNumPoints()
    {
        return points.Length;
    }

    public VertexPath GetPath()
    {
        return path;
    }

    public float GetT()
    {
        return t;
    }

    public void UpdateShape(Vector2[] newVertices)
    {
        points = newVertices;
    }
}