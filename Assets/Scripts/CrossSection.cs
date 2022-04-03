using UnityEngine;
using PathCreation;
using UnityEditor;

[ExecuteInEditMode]
public class CrossSection : MonoBehaviour
{
    // Consts
    private const string UNIT_CIRCLE = "<path d=\"M100 50C100 77.6142 77.6142 100 50 100C22.3858 100 0 77.6142 0 50C0 22.3858 22.3858 0 50 0C77.6142 0 100 22.3858 100 50Z\"/>";

    // Configuration
    [Range(0, 1)]
    [Tooltip("Position on the curve. From 0 to 1. 0 being the start, 1 being the end of the curve.")]
    public float t = 0;

    [Range(-180, 180)]
    [Tooltip("Rotation of the cross section. This rotation is perpendicular to the curve direction and curve normal.")]
    public float rotation = 0;

    [Tooltip("Scaling of the cross section.")]
    public Vector2 scale = Vector2.one;

    [Tooltip("The shape of the cross section will be read from the path variable in the svg file.")]
    public TextAsset SVGFile;

    // in order to ensure that we always have crossSectionData
    public ShapeData CrossSectionData
    {
        get
        {
            _crossSectionData = GetCrossSectionData();
            return _crossSectionData;
        }
    }

    //
    // --- Internal variables ---
    //

    private ShapeData _crossSectionData;
    private PathCreator _pathCreator;
    private PathCreator PathCreator
    {
        get
        {
            if (_pathCreator == null)
            {
                _pathCreator = GetComponentInParent<PathCreator>();
            }
            return _pathCreator;
        }
    }

    // Add a menu item to create custom GameObjects.
    // Priority 10 ensures it is grouped with the other menu items of the same kind
    // and propagated to the hierarchy dropdown and hierarchy context menus.
    [MenuItem("GameObject/Custom/Cross Section", false, 10)]
    static void CreateExtruder(MenuCommand menuCommand)
    {
        var parent = menuCommand.context as GameObject;
        if (parent == null)
        {
            return;
        }

        if (parent.GetComponent<Extruder>() == null)
        {
            return;
        }

        // Create a custom game object
        var index = parent.transform.childCount + 1 + "";
        var go = new GameObject($"Cross Section {index.PadLeft(2, '0')}");
        go.AddComponent<CrossSection>();

        // Ensure it gets reparented if this was a context click (otherwise does nothing)
        GameObjectUtility.SetParentAndAlign(go, parent);
        // Register the creation in the undo system
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
    }

    public void Update()
    {
        UpdateUnityTransform();
    }

    private ShapeData GetCrossSectionData()
    {
        // get the svg text with fallback
        string svgText;
        if (SVGFile == null)
        {
            svgText = UNIT_CIRCLE;
        }
        else
        {
            svgText = SVGFile.text;
        }

        var svgData = ProjectUtil.SVGStringTOSVGData(svgText);
        var crossSectionData = ProjectUtil.SVGToShapeData(svgData, rotation, scale, PathCreator, t);
        return crossSectionData;
    }

    private void UpdateUnityTransform()
    {
        // position
        transform.localPosition = PathCreator.path.GetPointAtTime(t);

        // rotation
        transform.localRotation = PathCreator.path.GetRotation(t) * Quaternion.Euler(0, 0, rotation);

        // scale
        transform.localScale = scale;
    }
}
