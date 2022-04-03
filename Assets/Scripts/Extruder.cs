using UnityEngine;
using PathCreation;
using System.Linq;
using static ProjectUtil;
using UnityEditor;
using mattatz.Triangulation2DSystem;
using Vector3Extension;

[ExecuteInEditMode]
[RequireComponent(typeof(PathCreator))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Extruder : MonoBehaviour
{
    // public
    public bool showWireMesh;
    public bool showVertexLabels;
    public bool showUserCrossSetions;

    // private 
    private PathCreator _pathCreator;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private CrossSection[] _crossSections;

    // Add a menu item to create custom GameObjects.
    // Priority 10 ensures it is grouped with the other menu items of the same kind
    // and propagated to the hierarchy dropdown and hierarchy context menus.
    [MenuItem("GameObject/Custom/Extruder", false, 10)]
    static void CreateExtruder(MenuCommand menuCommand)
    {
        // Create a custom game object
        var go = new GameObject("Extruder");
        go.AddComponent<Extruder>();
        var extruderMeshRenderer = go.GetComponent<MeshRenderer>();
        var extruderPathCreator = go.GetComponent<PathCreator>();
        extruderPathCreator.bezierPath.ControlPointMode = BezierPath.ControlMode.Automatic;

        // add the first cross section
        var cs1_go = new GameObject("Cross Section 01");
        cs1_go.transform.parent = go.transform;
        var cs1 = cs1_go.AddComponent<CrossSection>();
        cs1.t = 0;

        // add the second cross section
        var cs2_go = new GameObject("Cross Section 02");
        cs2_go.transform.parent = go.transform;
        var cs2 = cs2_go.AddComponent<CrossSection>();
        cs2.t = 1;

        // Ensure it gets reparented if this was a context click (otherwise does nothing)
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        // Register the creation in the undo system
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
    }

    private void GetReferences()
    {
        if (_pathCreator == null)
        {
            _pathCreator = GetComponentInParent<PathCreator>();
        }

        if (_meshFilter == null)
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }
    }

    private void Update()
    {
        GetReferences();
        RenderMesh();
    }

    private void RenderMesh()
    {
        _crossSections = GetComponentsInChildren<CrossSection>();
        if (_crossSections.Length <= 0)
        {
            return;
        }

        var shapes = CreateAllShapes();
        var mesh = CombineShapesIntoMesh(shapes, shapes[0], shapes[shapes.Length - 1]);
        _meshFilter.mesh = mesh;
    }

    private ShapeData[] GetCrossSections()
    {
        _crossSections = GetComponentsInChildren<CrossSection>();
        var cs = _crossSections
                    .Select(cs => cs.CrossSectionData)
                    .OrderBy(cs => cs.GetT())
                    .ToArray();
        return cs;
    }

    // Used to draw the vertices on the cross sections
    private void OnDrawGizmos()
    {
        var shapes = GetCrossSections();
        // var (shapes, startShape, endShape) = CreateAllShapes();

        if (shapes.Length <= 0)
        {
            return;
        }

        shapes = ShapeInterpolator.ExpandShapes(shapes);
        if (showUserCrossSetions)
        {
            foreach (var cs in shapes)
            {
                Vector3[] array = cs.Get3DPoints();
                for (int i = 0; i < array.Length; i++)
                {
                    Vector3 v = array[i];
                    Vector3 v2 = array[(i + 1) % array.Length];
                    Gizmos.DrawSphere(v, 0.03f);
                    Gizmos.DrawLine(v, v2);
                    if (showVertexLabels)
                    {
                        Handles.Label(v, $"{i}");
                    }
                }
            }
        }

        if (showWireMesh)
        {
            Gizmos.DrawWireMesh(_meshFilter.sharedMesh, -1, Vector3.zero, Quaternion.identity, Vector3.one);
        }
    }

    private ShapeData[] CreateAllShapes()
    {
        var path = _pathCreator.path;

        // Get all the cross sections and expand them
        var userShapes = GetCrossSections();
        userShapes = ShapeInterpolator.ExpandShapes(userShapes);

        var shapes = new ShapeData[path.NumPoints + userShapes.Length];
        for (int i = 0; i < path.NumPoints; i++)
        {
            // Get the point on the vertex path
            var point = path.GetPoint(i);

            // Find the time t of the point on the path
            var t = path.GetClosestTimeOnPath(point);

            // Find the cross sections that the point lies between using t
            var (startShape, endShape, t2) = GetCrossSections(userShapes, t);

            // Find the shape of the point using the cross sections using t2
            var middleShape = ShapeInterpolator.MorphShape(startShape, endShape, t2, t);

            // store the created shape
            shapes[i] = middleShape;
        }

        for (int i = 0; i < userShapes.Length; i++)
        {
            shapes[i + path.NumPoints] = userShapes[i];
        }

        shapes = shapes.OrderBy(s => s.GetT()).ToArray();

        return shapes;
    }

    private Mesh CombineShapesIntoMesh(ShapeData[] shapes, ShapeData start, ShapeData end)
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

        // start shape
        var startMesh = new Triangulation2D(Polygon2D.Contour(start.Get2DPoints())).Build();
        startMesh.vertices = startMesh.vertices.Select(v => (Vector2)v).ToArray().To3DPoints(_pathCreator.path, 0);
        startMesh.RecalculateBounds();
        startMesh.RecalculateNormals();

        // end shape
        var endMesh = new Triangulation2D(Polygon2D.Contour(end.Get2DPoints())).Build();
        endMesh.triangles = endMesh.triangles.Reverse().ToArray();
        endMesh.vertices = endMesh.vertices.Select(v => (Vector2)v).ToArray().To3DPoints(_pathCreator.path, 1);
        endMesh.RecalculateBounds();
        endMesh.RecalculateNormals();

        // Create the mesh
        var mesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles,
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var combine = new CombineInstance[3];
        combine[0].mesh = startMesh;
        combine[1].mesh = mesh;
        combine[2].mesh = endMesh;

        var finalMesh = new Mesh();
        finalMesh.CombineMeshes(combine, true, false, false);

        return finalMesh;
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

        // before the first cross section
        if (t < crossSections[0].GetT())
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

        // after the last cross section
        return (crossSections[crossSections.Length - 1], crossSections[crossSections.Length - 1], 0); // todo
    }
}