using UnityEngine;
using PathCreation;
using System.Linq;
using MinByExtension;
using Vector3Extension;


[ExecuteInEditMode]
[RequireComponent(typeof(PathCreator))]
public class Extruder : MonoBehaviour
{
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

    private void Update()
    {
        // CreateCombinedMesh();
    }

    private void CreateCombinedMesh()
    {
        var pathCreator = GetPathCreator();
        var path = pathCreator.path;
        var crossSections = GetCrossSections();
        var numVertices = CountVertices(path);

        var shapes = new Vector3[numVertices][];
        var vertices = new Vector3[numVertices];
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
            var middleShape = ShapeInterpolator.GetShape(crossSection1.Get2DPoints(), crossSection2.Get2DPoints(), t2);

            // transform the shape to 3D
            var middleShapePoints3D = middleShape.To3DPoints(path, t);

            // Get the triangle indices from the combination of 2 shapes
            var triangleIndices = MatchVertices(Enumerable.Range(0, middleShape.Length).ToArray(),
             Enumerable.Range(middleShape.Length, middleShape.Length * 2).ToArray());

            // Find the previous shape to connect to
            Vector3[] prevShape;
            if (i == 0)
            {
                prevShape = crossSections[0].Get3DPoints();
            }
            else
            {
                prevShape = shapes[i - 1];
            }

            // Create the mesh
            var mesh = new Mesh
            {
                vertices = prevShape.Concat(middleShapePoints3D).ToArray(),
                triangles = triangleIndices,
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            shapes[i] = middleShapePoints3D;

            // draw the mesh for debugging
            Gizmos.DrawWireMesh(mesh, -1, Vector3.zero, Quaternion.identity, Vector3.one);
        }
    }

    // Given two aligned arrays of the shapes vertices' indices, 
    // creates the triangle indices for one segment of the mesh
    private int[] MatchVertices(int[] shape1, int[] shape2)
    {
        var shapeLength = shape1.Length;
        var triangles = new int[shapeLength * 2 * 3]; // 2 triangles/face, 3 indices/triangle
        for (var i = 0; i < shapeLength; i++)
        {
            var nextIndex = (i + 1) % shapeLength;
            var faceTriangles = MakeTriangles(new int[] { shape1[i], shape2[i] },
                                            new int[] { shape1[nextIndex], shape2[nextIndex] });

            var faceTrianglesLenght = faceTriangles.Length;
            // Copy the indices of the face triangles to the main triangles array
            for (var j = 0; j < faceTrianglesLenght; j++)
            {
                triangles[i * faceTrianglesLenght + j] = faceTriangles[j];
            }
        }

        return triangles;
    }

    private int[] MakeTriangles(int[] edge1, int[] edge2)
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

    private Mesh Create2DMesh(Vector2[] vertices)
    {
        var vertices3D = System.Array.ConvertAll<Vector2, Vector3>(vertices, v => v);
        var triangulator = new Triangulator(vertices);
        var triangleIndices = triangulator.Triangulate();

        var mesh = new Mesh
        {
            vertices = vertices3D,
            triangles = triangleIndices,
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void OnDrawGizmos()
    {

        CreateCombinedMesh();
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

    private int CountVertices(VertexPath path)
    {
        var numVertices = 0;
        for (int i = 0; i < path.NumPoints; i++)
        {
            // Get the point on the vertex path
            var point = path.GetPoint(i);

            // Find the time t of the point
            var t = path.GetClosestTimeOnPath(point);

            // Find the cross sections that the point lies between
            var (crossSection1, crossSection2, t2) = GetCrossSections(t);

            // Find the shape of the point using the cross sections
            var shape = ShapeInterpolator.GetShape(crossSection1.Get2DPoints(), crossSection2.Get2DPoints(), t2);

            // Add the number of vertices to the total count
            numVertices += shape.Length;
        }

        return numVertices;
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