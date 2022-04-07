using UnityEngine;
using PathCreation;
using System.Linq;
using static ProjectUtil;
using UnityEditor;
using mattatz.Triangulation2DSystem;
using Vector3Extension;
using System;

[ExecuteInEditMode]
[RequireComponent(typeof(PathCreator))]
public class Extruder : MonoBehaviour
{
    // public
    public MeshFilter target;
    public AnimationCurve[] interpolationCurves = new AnimationCurve[0];
    public Color32 minColor = new Color32(60, 0, 0, 255);
    public Color32 maxColor = new Color32(0, 0, 60, 255);
    public bool globalScaleForColor = false;
    public bool showWireMesh;
    public bool showVertexLabels;
    public bool showUserCrossSetions;
    public ShapeData[] vertexShapes;

    // private 
    private PathCreator _pathCreator;
    private CrossSection[] _crossSections;

    // Add a menu item to create custom GameObjects.
    // Priority 10 ensures it is grouped with the other menu items of the same kind
    // and propagated to the hierarchy dropdown and hierarchy context menus.
    [MenuItem("GameObject/Custom/Extruder", false, 10)]
    static void CreateExtruder(MenuCommand menuCommand)
    {
        // Create the output object
        var target_go = new GameObject("Output");
        target_go.AddComponent<MeshFilter>();
        var targetMeshRenderer = target_go.AddComponent<MeshRenderer>();
        var defaultMaterial = Resources.Load("DemoDepthMaterial", typeof(Material)) as Material;
        targetMeshRenderer.material = defaultMaterial;

        // Create a custom game object
        var go = new GameObject("Extruder");
        var extruder = go.AddComponent<Extruder>();
        var extruderMeshRenderer = go.GetComponent<MeshRenderer>();
        var extruderPathCreator = go.GetComponent<PathCreator>();
        extruderPathCreator.bezierPath.ControlPointMode = BezierPath.ControlMode.Automatic;
        extruder.target = target_go.GetComponent<MeshFilter>();

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

    /// Get the references from unity
    private void GetReferences()
    {
        if (_pathCreator == null)
        {
            _pathCreator = GetComponentInParent<PathCreator>();
        }
    }

    private void Update()
    {
        GetReferences();
        ConstraintInterpolationCurvesLength();
        RenderMesh();
    }

    /// Constraints the length of the interpolation curves array.
    /// The size is #crossSections - 1
    private void ConstraintInterpolationCurvesLength()
    {
        var len = interpolationCurves.Length;
        var wantedLength = GetComponentsInChildren<CrossSection>().Length - 1;

        if (len == wantedLength)
        {
            return;
        }

        if (len > wantedLength)
        {
            var newCurves = new AnimationCurve[wantedLength];
            for (int i = 0; i < wantedLength; i++)
            {
                newCurves[i] = interpolationCurves[i];
            }
            interpolationCurves = newCurves;
        }

        if (len < wantedLength)
        {
            var newCurves = new AnimationCurve[wantedLength];
            for (int i = 0; i < len; i++)
            {
                newCurves[i] = interpolationCurves[i];
            }

            for (int i = len; i < wantedLength; i++)
            {
                newCurves[i] = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 1) });
            }
            interpolationCurves = newCurves;
        }
    }

    /// Renders the mesh created from the procedure
    private void RenderMesh()
    {
        _crossSections = GetComponentsInChildren<CrossSection>();
        if (_crossSections.Length <= 0)
        {
            return;
        }

        var shapes = CreateAllShapes();
        var mesh = CombineShapesIntoMesh(shapes);
        vertexShapes = shapes;

        if (target != null)
        {
            target.mesh = mesh;
        }
    }

    // Gets all user defined crossSection data from the game objects
    public ShapeData[] GetCrossSections()
    {
        _crossSections = GetComponentsInChildren<CrossSection>();
        var cs = _crossSections
                    .Select(cs => cs.CrossSectionData)
                    .OrderBy(cs => cs.GetT())
                    .ToArray();
        return cs;
    }

    // Used to draw the vertices on the cross sections
    // Draws the vertices on the cross sections
    // Draws the cross sections
    // Draws the vertex indices
    private void OnDrawGizmos()
    {
        var shapes = GetCrossSections();

        if (shapes.Length <= 0)
        {
            return;
        }

        var path = _pathCreator.path;

        // viewPoint
        var firstPoint = path.GetPointAtTime(0);
        var direction = path.GetDirection(0);
        var viewPoint = firstPoint - direction;

        shapes = ShapeInterpolator.ExpandShapes(shapes, viewPoint);
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

        if (showWireMesh && target != null)
        {
            Gizmos.DrawWireMesh(target.sharedMesh, -1, Vector3.zero, Quaternion.identity, Vector3.one);
        }
    }

    // Creates all intermediate shapes resulting from the sampling of the curve and cross section interpolations
    private ShapeData[] CreateAllShapes()
    {
        var path = _pathCreator.path;

        // viewPoint
        var firstPoint = path.GetPointAtTime(0);
        var direction = path.GetDirection(0);
        var viewPoint = firstPoint - direction;

        // Get all the cross sections and expand them
        var userShapes = GetCrossSections();
        userShapes = ShapeInterpolator.ExpandShapes(userShapes, firstPoint);

        var shapes = new ShapeData[path.NumPoints + userShapes.Length];
        for (int i = 0; i < path.NumPoints; i++)
        {
            // Get the point on the vertex path
            var point = path.GetPoint(i);

            // Find the time t of the point on the path
            var t = path.GetClosestTimeOnPath(point);

            // Find the cross sections that the point lies between using t
            var (startShape, endShape, t2) = GetCrossSections(userShapes, t);
            var startShapeIndex = Array.IndexOf(userShapes, startShape);

            // Array.IndexOf can return -1 in case it doesn't find the element. In which case fallback to the first interpolation curve
            // This doesn't matter because if the IndexOf cannot find the index, that means its before the first shape.
            // Another thing that could happen is if the start shape is the end shape, that means the its after the last shape.
            // In both cases the t value will be 0, so no interpolation will happen.
            var interpolationCurve = interpolationCurves[0];
            if (startShapeIndex >= 0 && startShapeIndex < interpolationCurves.Length)
            {
                interpolationCurve = interpolationCurves[startShapeIndex];
            }

            // Find the shape at the point i, using the cross sections using t2
            var middleShape = ShapeInterpolator.MorphShape(startShape, endShape, t2, t, interpolationCurve);

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

    // Combines all shapes into a single mesh
    private Mesh CombineShapesIntoMesh(ShapeData[] shapes)
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
        var start = shapes[0];
        var startMesh = new Triangulation2D(Polygon2D.Contour(start.Get2DPoints())).Build();
        startMesh.vertices = startMesh.vertices.Select(v => (Vector2)v).ToArray().To3DPoints(_pathCreator.path, 0);
        startMesh.RecalculateBounds();
        startMesh.RecalculateNormals();

        // end shape
        var end = shapes[shapes.Length - 1];
        var endMesh = new Triangulation2D(Polygon2D.Contour(end.Get2DPoints())).Build();
        endMesh.triangles = endMesh.triangles.Reverse().ToArray();
        endMesh.vertices = endMesh.vertices.Select(v => (Vector2)v).ToArray().To3DPoints(_pathCreator.path, 1);
        endMesh.RecalculateBounds();
        endMesh.RecalculateNormals();

        // Create the mesh
        var sideMesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles,
        };

        // Color the meshes
        (sideMesh.colors32, startMesh.colors32, endMesh.colors32) = ColorMeshes(shapes, startMesh.vertices, endMesh.vertices);

        // combine the meshes
        var combine = new CombineInstance[3];
        combine[0].mesh = startMesh;
        combine[1].mesh = sideMesh;
        combine[2].mesh = endMesh;

        var finalMesh = new Mesh();
        finalMesh.CombineMeshes(combine, true, false, false);

        // finalMesh.vertices
        finalMesh.RecalculateBounds();
        finalMesh.RecalculateNormals();
        finalMesh.RecalculateTangents();
        finalMesh.RecalculateUVDistributionMetrics();

        return finalMesh;
    }

    private (Color32[] side, Color32[] start, Color32[] end) ColorMeshes(ShapeData[] shapes, Vector3[] startVertices, Vector3[] endVertices)
    {
        // colors of the side mesh
        var (sideMeshColors, sideMeshMin, sideMeshMax) = AssignColorsToSide(shapes, minColor, maxColor, globalScaleForColor);

        // colors of the start and end mesh
        var startMeshColors = AssignColorsToEnds(startVertices, minColor, maxColor, sideMeshMin, sideMeshMax, globalScaleForColor);
        var endMeshColors = AssignColorsToEnds(endVertices, minColor, maxColor, sideMeshMin, sideMeshMax, globalScaleForColor);

        return (sideMeshColors, startMeshColors, endMeshColors);
    }

    private static (Color32[], float, float) AssignColorsToSide(ShapeData[] shapes, Color32 color1, Color32 color2, bool global)
    {
        var magnitudes = shapes
                        .SelectMany(s => s.Get2DPoints())
                        .Select(v => v.magnitude);
        var max = magnitudes.Max();
        var min = magnitudes.Min();

        if (global)
        {
            magnitudes = magnitudes
                            .Select(t => (t - min) / (max - min));
        }

        var colors = magnitudes
                            .Select(t => Color32.Lerp(color1, color2, t))
                            .ToArray();
        return (colors, min, max);
    }

    private static Color32[] AssignColorsToEnds(Vector3[] points, Color32 color1, Color32 color2, float globalMin, float globalMax, bool global)
    {
        var center = BoundingBoxCenter(points);
        var magnitudes = points
                    .Select(p => p - center)
                    .Select(p => p.magnitude);

        if (global)
        {
            magnitudes = magnitudes
                    .Select(m => (m) / (globalMax));
        }

        var colors = magnitudes
                    .Select(t => Color32.Lerp(color1, color2, t))
                    .ToArray();
        return colors;
    }

    private static Vector3 BoundingBoxCenter(Vector3[] points)
    {
        //x
        var xs = points.Select(p => p.x);
        var minX = xs.Min();
        var maxX = xs.Max();

        //y
        var ys = points.Select(p => p.y);
        var minY = ys.Min();
        var maxY = ys.Max();

        //z
        var zs = points.Select(p => p.z);
        var minZ = zs.Min();
        var maxZ = zs.Max();

        return new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, (minZ + maxZ) / 2f);
    }

    // Given two aligned arrays of the shapes vertices' indices, 
    // creates the triangle indices for one side segment of the mesh
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

    // Given two edges, creates the triangles for the rectangle
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
            throw new System.Exception();
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
        return (crossSections[crossSections.Length - 1], crossSections[crossSections.Length - 1], 0);
    }
}