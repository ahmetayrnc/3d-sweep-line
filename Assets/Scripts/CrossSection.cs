using UnityEngine;
using PathCreation;

[ExecuteInEditMode]
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

    public TextAsset SVGFile;

    public string pathString;

    //
    // --- Internal variables ---
    //

    // Actual data that reprsents a cross section
    private ShapeData _crossSectionData;
    // Reference to the path creator object
    private PathCreator _pathCreator;

    //
    // --- Public Methods ---
    //
    public ShapeData GetCrossSectionData()
    {
        var svgData = ProjectUtil.SVGStringTOSVGData(SVGFile.text);
        _crossSectionData = ProjectUtil.SVGToShapeData(svgData, rotation, scale, _pathCreator, t);
        return _crossSectionData;
    }

    private void Awake()
    {
        _pathCreator = GetComponentInParent<PathCreator>();
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
