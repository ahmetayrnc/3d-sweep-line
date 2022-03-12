using UnityEngine;
using PathCreation;
using System.Linq;
using static ProjectUtil;

[ExecuteInEditMode]
[RequireComponent(typeof(PathCreator))]
[RequireComponent(typeof(MeshFilter))]
public class Extruder : MonoBehaviour
{
    public int[] Triangles;
    private PathCreator _pathCreator;
    private ShapeData[] _crossSections;
    private MeshFilter _meshFilter;

    private void Awake()
    {
        if (_pathCreator == null)
        {
            _pathCreator = GetComponentInParent<PathCreator>();
        }

        if (_meshFilter == null)
        {
            _meshFilter = GetComponent<MeshFilter>();
        }
    }

    private ShapeData[] GetCrossSections()
    {
        _crossSections = GetComponentsInChildren<CrossSection>().Select(cs => cs.GetCrossSectionData()).ToArray();
        return _crossSections;
    }

    private void OnDrawGizmos()
    {
        var mesh = CreatePathMesh();
        _meshFilter.mesh = mesh;

        Triangles = mesh.triangles;

        // Gizmos.DrawWireMesh(mesh, -1, Vector3.zero, Quaternion.identity, Vector3.one);
    }

    private Mesh CreatePathMesh()
    {
        var path = _pathCreator.path;

        // Get all the cross sections and expand them
        var crossSections = GetCrossSections();
        crossSections = ShapeInterpolator.ExpandShapes(crossSections);

        var shapes = new ShapeData[path.NumPoints];
        for (int i = 0; i < path.NumPoints; i++)
        {
            // Get the point on the vertex path
            var point = path.GetPoint(i);

            // Find the time t of the point on the path
            var t = path.GetClosestTimeOnPath(point);

            // Find the cross sections that the point lies between using t
            var (startShape, endShape, t2) = GetCrossSections(crossSections, t);

            // Find the shape of the point using the cross sections using t2
            var middleShape = ShapeInterpolator.MorphShape(startShape, endShape, t2, t);

            // store the created shape
            shapes[i] = middleShape;
        }

        var mesh = CreateMesh(shapes, crossSections[0], crossSections[crossSections.Length - 1]);
        return mesh;
    }

    private static Mesh CreateMesh(ShapeData[] shapes, ShapeData start, ShapeData end)
    {
        // Create the triangles for the middle sections
        var shapeTriangles = new int[shapes.Length - 1][];
        for (var i = 0; i < shapes.Length - 1; i++)
        {
            var shapeLength = shapes[i].GetNumPoints();

            shapeTriangles[i] = MakeTrianglesForShape(Enumerable.Range(i * shapeLength, shapeLength).ToArray(),
                    Enumerable.Range((i + 1) * shapeLength, shapeLength).ToArray());
        }

        var vertices = ConcatArrays(shapes.Select(s => s.Get3DPoints()).ToArray());
        var triangles = ConcatArrays(shapeTriangles);

        // create the triangles for the end sections
        var triangulator = new Triangulator(start.Get2DPoints());
        var startShapeTriangles = triangulator.Triangulate();
        triangulator = new Triangulator(end.Get2DPoints());
        var endShapeTriangles = triangulator.Triangulate();

        // the result of the triangluation has the indices starting from 0, convert them to the correct ones.
        endShapeTriangles = endShapeTriangles.Select(i => vertices.Length - i - 1).ToArray();

        // Add the start and end triangles
        triangles = ConcatArrays(triangles, startShapeTriangles, endShapeTriangles);

        // Create the mesh
        var mesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles,
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    // Given two aligned arrays of the shapes vertices' indices, 
    // creates the triangle indices for one segment of the mesh
    private static int[] MakeTrianglesForShape(int[] shape1, int[] shape2)
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
    private static int[] MakeTrianglesForFace(int[] edge1, int[] edge2)
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

    // Find the cross sections that are to the left and to the right of the point t
    private (ShapeData a, ShapeData b, float t2) GetCrossSections(ShapeData[] crossSections, float t)
    {
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
            if (crossSection.GetT() < t)
            {
                continue;
            }

            var prevCrossSection = crossSections[i - 1];
            var t2 = Mathf.InverseLerp(prevCrossSection.GetT(), crossSection.GetT(), t);
            return (prevCrossSection, crossSection, t2);
        }

        // The t value is outside the defined cross sections.
        return (crossSections[0], crossSections[0], 0); // todo
    }
}