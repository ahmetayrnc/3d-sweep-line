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

    [Range(3, 8)]
    public int numPoints = 3;

    [Range(0, 2 * Mathf.PI)]
    public float rotation = 0;

    // Internal variables
    private PathCreator _pathCreator;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    // --- Public Methods ---

    // Uses a 2D coordinate system to describe the shape
    public Vector2[] Get2DPoints()
    {
        var pathCreator = GetPathCreator();
        var path = pathCreator.path;
        var direction = path.GetDirection(t, EndOfPathInstruction.Stop);

        var points3d = Get3DPoints();
        var points2d = points3d.Select(p => (Vector2)Vector3.ProjectOnPlane(p, direction)).ToArray();
        return points2d;
    }

    // Uses the 2D coordinate systems and the position on the path as the z value
    public Vector3[] GetPathPoints()
    {
        var points2d = Get2DPoints();
        var pathPoints = points2d.Select(p => new Vector3(p.x, p.y, t)).ToArray();
        return pathPoints;
    }

    // Uses the actual 3D coordinate system
    public Vector3[] Get3DPoints()
    {
        var meshFilter = GetMeshFilter();
        var points = meshFilter.sharedMesh.vertices;
        return points;
    }

    public Vector3 GetCenterPoint()
    {
        return transform.position;
    }

    // Private Methods
    private PathCreator GetPathCreator()
    {
        if (_pathCreator == null)
        {
            _pathCreator = GetComponentInParent<PathCreator>();
        }
        return _pathCreator;
    }

    private MeshFilter GetMeshFilter()
    {
        if (_meshFilter == null)
        {
            _meshFilter = GetComponent<MeshFilter>();
        }
        return _meshFilter;
    }

    private void Update()
    {
        UpdatePosition();
        UpdateMesh();
        UpdateDirection();
    }

    private void UpdatePosition()
    {
        var pathCreator = GetPathCreator();
        var point = pathCreator.path.GetPointAtTime(t, EndOfPathInstruction.Stop);
        transform.position = point;
    }

    private void UpdateMesh()
    {
        // Polygon vertices
        var angle = 2 * Mathf.PI / numPoints;
        var vertices = new Vector2[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            var x = Mathf.Sin(rotation + i * angle);
            var y = Mathf.Cos(rotation + i * angle);
            vertices[i] = new Vector2(x, y);
        }

        // Mesh creation
        var vertices3D = vertices.Select(v => v.To3DPoint(GetPathCreator().path, t)).ToArray();
        // var vertices3D = System.Array.ConvertAll<Vector2, Vector3>(vertices, v => v);
        var triangulator = new Triangulator(vertices);
        var triangleIndices = triangulator.Triangulate();

        var mesh = new Mesh
        {
            vertices = vertices3D,
            triangles = triangleIndices,
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var meshFilter = GetMeshFilter();

        meshFilter.mesh = mesh;
    }

    private void UpdateDirection()
    {
        var pathCreator = GetPathCreator();
        var direction = pathCreator.path.GetDirection(t, EndOfPathInstruction.Stop);
        // transform.rotation = Quaternion.LookRotation(direction);
    }

    private void OnDrawGizmos()
    {
        var meshFilter = GetMeshFilter();
        Gizmos.DrawWireMesh(meshFilter.sharedMesh, -1, transform.position, transform.rotation, transform.localScale);
    }
}
