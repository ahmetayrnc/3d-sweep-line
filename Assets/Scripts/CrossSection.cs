using UnityEngine;
using PathCreation;
using System.Linq;
using Vector3Extension;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
public class CrossSection : MonoBehaviour
{
    // Configuration
    [Range(0, 1)]
    [Tooltip("Position on the curve. From 0 to 1. 0 being the start, 1 being the end of the curve.")]
    public float t = 0;

    [Range(3, 12)]
    public int numPoints = 3;

    [Range(-180, 180)]
    [Tooltip("Rotation of the cross section. This rotation is perpendicular to the curve direction and curve normal.")]
    public float rotation = 0;

    [Tooltip("Scaling of the cross section.")]
    public Vector2 scale = Vector2.one;

    //
    // --- Internal variables ---
    //

    // Actual data that reprsents a cross section
    private ShapeData _crossSectionData;
    // Reference to the path creator object
    private PathCreator _pathCreator;
    // Mesh filter to store the mesh representing the cross section
    private MeshFilter _meshFilter;
    // Renderer of the mesh
    private MeshRenderer _meshRenderer;

    //
    // --- Public Methods ---
    //
    public ShapeData GetCrossSectionData()
    {
        _crossSectionData = new ShapeData(CreatePoints(), _pathCreator.path, t);
        return _crossSectionData;
    }

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

        _crossSectionData = new ShapeData(CreatePoints(), _pathCreator.path, t);
    }

    private void Update()
    {
        UpdateUnityTransform();
    }

    private void UpdateUnityTransform()
    {
        _crossSectionData = new ShapeData(CreatePoints(), _pathCreator.path, t);

        // position
        transform.localPosition = _pathCreator.path.GetPointAtTime(t);

        // rotation
        transform.localRotation = _pathCreator.path.GetRotation(t) * Quaternion.Euler(0, 0, rotation);

        // scale
        transform.localScale = scale;
    }

    private Vector2[] CreatePoints()
    {
        var angle = 2 * Mathf.PI / numPoints;
        var vertices = new Vector2[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            var x = Mathf.Sin(i * angle);
            var y = Mathf.Cos(i * angle);
            vertices[i] = new Vector2(x, y);
        }

        vertices = vertices.Select(v => Vector2.Scale(v, scale)).Select(v => (Vector2)(Quaternion.Euler(0, 0, rotation) * v)).ToArray();

        return vertices;
    }

    // DEBUGGING PURPOSES
    // private Mesh CreateMesh()
    // {
    //     var vertices = CreatePoints();

    //     // Mesh creation
    //     var vertices3D = vertices.Select(v => v.To3DPoint(_pathCreator.path, t)).ToArray();
    //     var triangulator = new Triangulator(vertices);
    //     var triangleIndices = triangulator.Triangulate();

    //     var mesh = new Mesh
    //     {
    //         vertices = vertices3D,
    //         triangles = triangleIndices,
    //     };

    //     mesh.RecalculateNormals();
    //     mesh.RecalculateBounds();

    //     return mesh;
    // }

    // private void OnDrawGizmos()
    // {
    //     var mesh = CreateMesh();

    //     for (var i = 0; i < mesh.vertices.Length; i++)
    //     {
    //         var v = mesh.vertices[i];
    //         // Handles.Label(v, $"{i}");
    //     }

    //     Gizmos.DrawWireMesh(mesh, -1, Vector3.zero, Quaternion.identity, Vector3.one);
    // }
}
