using UnityEngine;
using PathCreation;
using System.Linq;
using MinByExtension;
using Vector3Extension;
using System;

[ExecuteInEditMode]
[RequireComponent(typeof(PathCreator))]
public class Extruder : MonoBehaviour
{
    public int[] Triangles;
    private PathCreator _pathCreator;
    private CrossSection[] _crossSections;

    private CrossSection[] GetCrossSections()
    {
        if (_crossSections == null)
        {
            _crossSections = GetComponentsInChildren<CrossSection>();
        }
        return _crossSections;
    }

    private PathCreator GetPathCreator()
    {
        if (_pathCreator == null)
        {
            _pathCreator = GetComponentInParent<PathCreator>();
        }
        return _pathCreator;
    }

    private void OnDrawGizmos()
    {
        CreateCombinedMesh();
    }

    private void CreateCombinedMesh()
    {
        var pathCreator = GetPathCreator();
        var path = pathCreator.path;
        var crossSections = GetCrossSections();

        var shapes = new Vector3[path.NumPoints][];
        for (int i = 0; i < path.NumPoints; i++)
        {
            // Get the point on the vertex path
            var point = path.GetPoint(i);
            Gizmos.DrawSphere(point, 0.05f);

            // Find the time t of the point on the path
            var t = path.GetClosestTimeOnPath(point);

            // Find the cross sections that the point lies between using t
            var (crossSection1, crossSection2, t2) = GetCrossSections(t);

            // Find the shape of the point using the cross sections using t2
            var middleShape = ShapeInterpolator.GetShape(crossSection1.Get2DPoints(), crossSection2.Get2DPoints(), t2).To3DPoints(path, t);

            // store the created shape
            shapes[i] = middleShape;
        }

        var mesh = CreateMesh(shapes);
        Gizmos.DrawMesh(mesh, -1, Vector3.zero, Quaternion.identity, Vector3.one);
    }

    private Mesh CreateMesh(Vector3[][] shapes)
    {
        var triangles = new int[shapes.Length - 1][];
        for (var i = 0; i < shapes.Length - 1; i++)
        {
            var shape = shapes[i];
            var shapeLength = shape.Length;

            triangles[i] = MakeTrianglesForShape(Enumerable.Range(i * shapeLength, shapeLength).ToArray(),
                    Enumerable.Range((i + 1) * shapeLength, shapeLength).ToArray());
        }

        // Create the mesh
        var mesh = new Mesh
        {
            vertices = ConcatArrays(shapes),
            triangles = ConcatArrays(triangles),
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        Triangles = ConcatArrays(triangles);

        return mesh;
    }

    // Given two aligned arrays of the shapes vertices' indices, 
    // creates the triangle indices for one segment of the mesh
    private int[] MakeTrianglesForShape(int[] shape1, int[] shape2)
    {
        var shapeLength = shape1.Length;
        var faces = new int[shapeLength][];

        for (var i = 0; i < shapeLength; i++)
        {
            var nextIndex = (i + 1) % shapeLength;
            var faceTriangles = MakeTrianglesForFace(new int[] { shape1[i], shape2[i] },
                                            new int[] { shape1[nextIndex], shape2[nextIndex] });
            faces[i] = faceTriangles;
        }

        var triangles = ConcatArrays(faces);
        return triangles;
    }

    // TODO: instead of int[] use a struct EDGE
    private int[] MakeTrianglesForFace(int[] edge1, int[] edge2)
    {
        var triangles = new int[6];

        // first triangle
        triangles[0] = edge1[0];
        triangles[1] = edge1[1];
        triangles[2] = edge2[1];

        // second triangle
        triangles[3] = edge1[0];
        triangles[4] = edge2[1];
        triangles[5] = edge2[0];

        return triangles;
    }

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

    private Vector3 GetClosestPoint(Vector3[] points, Vector3 reference)
    {
        if (points.Length == 0)
        {
            return Vector3.zero; //TODO
        }

        var closestPoint = points.MinBy(p => (p - reference).sqrMagnitude);
        return closestPoint;
    }

    private (CrossSection a, CrossSection b, float t2) GetCrossSections(float t)
    {
        var crossSections = GetCrossSections();

        // No cross sections defined, we can't extrude a path.
        if (crossSections.Length == 0)
        {
            throw new System.Exception(); // todo
        }

        // Only 1 cross section is defined, use the same cross sections for interpolation.
        if (crossSections.Length == 1)
        {
            return (crossSections[0], crossSections[0], 0);
        }

        // Find the cross sections 
        for (int i = 1; i < crossSections.Length; i++)
        {
            var crossSection = crossSections[i];
            if (crossSection.t < t)
            {
                continue;
            }

            var prevCrossSection = crossSections[i - 1];
            var t2 = Mathf.InverseLerp(prevCrossSection.t, crossSection.t, t);
            return (prevCrossSection, crossSection, t2);
        }

        // The t value is outside the defined cross sections.
        return (crossSections[0], crossSections[0], 0); // todo
    }
}