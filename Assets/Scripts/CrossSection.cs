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

    public Vector2[] coords2D;
    public Vector3[] coord3D;

    // Internal variables
    private PathCreator _pathCreator;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    // --- Public Methods ---

    // Uses a 2D coordinate system to describe the shape
    public Vector2[] Get2DPoints()
    {
        return coords2D;
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
        var pathCreator = GetPathCreator();
        var path = pathCreator.path;

        var points2D = Get2DPoints();
        var points3D = points2D.Select(p => p.To3DPoint(path, t)).ToArray();

        return points3D;
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
        UpdateMesh();

        coords2D = Get2DPoints();
        coord3D = Get3DPoints();
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

        coords2D = vertices;

        // Mesh creation
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

        var meshFilter = GetMeshFilter();

        meshFilter.mesh = mesh;
    }

    private void OnDrawGizmos()
    {
        // var meshFilter = GetMeshFilter();
        // meshFilter.sharedMesh.vertices = Get3DPoints();
        // Gizmos.DrawWireMesh(meshFilter.sharedMesh, -1, Vector3.zero, Quaternion.identity, transform.localScale);
    }
}
