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
    public float t = 0;

    [Range(3, 12)]
    public int numPoints = 3;

    [Range(-180, 180)]
    public float rotation = 0;

    public Vector2 scale = Vector2.one;

    // public Vector2[] coords2D;
    // public Vector3[] coord3D;

    // Internal variables
    private ShapeData _crossSectionData;
    private PathCreator _pathCreator;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    // --- Public Methods ---
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
        _crossSectionData = new ShapeData(CreatePoints(), _pathCreator.path, t);
        transform.position = _pathCreator.path.GetPointAtTime(t);
        transform.rotation = _pathCreator.path.GetRotation(t);
        transform.rotation *= Quaternion.Euler(0, 0, rotation);
        transform.localScale = scale;

        // coords2D = _crossSectionData.Get2DPoints();
        // coord3D = _crossSectionData.Get3DPoints();
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

        vertices = vertices.Select(v => (Vector2)(Quaternion.Euler(0, 0, rotation) * v)).Select(v => Vector2.Scale(v, scale)).ToArray();

        return vertices;
    }

    // DEBUGGING PURPOSES
    private Mesh CreateMesh()
    {
        var vertices = CreatePoints();
        // coords2D = vertices;

        // Mesh creation
        var vertices3D = vertices.Select(v => v.To3DPoint(_pathCreator.path, t)).ToArray();
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
        var mesh = CreateMesh();

        for (var i = 0; i < mesh.vertices.Length; i++)
        {
            var v = mesh.vertices[i];
            // Handles.Label(v, $"{i}");
        }

        Gizmos.DrawWireMesh(mesh, -1, Vector3.zero, Quaternion.identity, Vector3.one);
    }
}
