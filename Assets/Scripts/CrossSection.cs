using UnityEngine;
using PathCreation;
using System.Linq;
using Vector3Extension;
using SVGMeshUnity;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
public class CrossSection : MonoBehaviour
{
    // Configuration
    [Range(0, 1)]
    [Tooltip("Position on the curve. From 0 to 1. 0 being the start, 1 being the end of the curve.")]
    public float t = 0;

    [Range(-180, 180)]
    [Tooltip("Rotation of the cross section. This rotation is perpendicular to the curve direction and curve normal.")]
    public float rotation = 0;

    [Tooltip("Scaling of the cross section.")]
    public Vector2 scale = Vector2.one;

    public Vector2[] pointsPublic;

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

    public string pathString;

    //
    // --- Public Methods ---
    //
    public ShapeData GetCrossSectionData()
    {
        var svg = new SVGData();
        svg.Path(pathString);

        _crossSectionData = SVGToShapeData(svg);
        return _crossSectionData;
    }

    private ShapeData SVGToShapeData(SVGData svg)
    {
        var svgMesh = new SVGMesh();
        var mesh = svgMesh.FillMesh(svg);

        // convert to vector2
        var vertices = mesh.vertices.Select(v => (Vector2)v);

        // Scale the shape between -1 and 1
        var min_x = vertices.Select(v => v.x).Min();
        var min_y = vertices.Select(v => v.y).Min();
        var max_x = vertices.Select(v => v.x).Max();
        var max_y = vertices.Select(v => v.y).Max();

        vertices = vertices.Select(v =>
        {
            return new Vector2(
                2.0f * (v.x - min_x) / (max_x - min_x) - 1.0f,
                2.0f * (v.y - min_y) / (max_y - min_y) - 1.0f);
        }).ToArray();

        // scaling

        // rotation
        vertices = vertices.Select(v => (Vector2)(Quaternion.Euler(0, 0, rotation) * v));

        //scale
        vertices = vertices.Select(v => v * scale);

        // Make sure the shape is in clockwise order
        if ((vertices.ElementAt(1).x - vertices.ElementAt(0).x) * (vertices.ElementAt(1).y + vertices.ElementAt(0).y) < 0)
        {
            vertices = vertices.Reverse().ToArray();
        }

        pointsPublic = vertices.ToArray();
        return new ShapeData(pointsPublic, _pathCreator.path, t);
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
    }

    private void Update()
    {
        UpdateUnityTransform();
    }

    private void UpdateUnityTransform()
    {
        _crossSectionData = GetCrossSectionData();

        // position
        transform.localPosition = _pathCreator.path.GetPointAtTime(t);

        // rotation
        transform.localRotation = _pathCreator.path.GetRotation(t) * Quaternion.Euler(0, 0, rotation);

        // scale
        transform.localScale = scale;
    }
}
