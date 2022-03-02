using UnityEngine;
using PathCreation;

[ExecuteInEditMode]
[RequireComponent(typeof(PathCreator))]
public class Extruder : MonoBehaviour
{
    private PathCreator _pathCreator;
    private CrossSection[] _crossSections;

    private CrossSection[] GetCrossSections()
    {
        if (_crossSections == null)
        {
            _crossSections = GetComponentsInChildren<CrossSection>();
        }
        return _crossSections;
    }

    private PathCreator GetPathCreator()
    {
        if (_pathCreator == null)
        {
            _pathCreator = GetComponentInParent<PathCreator>();
        }
        return _pathCreator;
    }

    private void Update()
    {
        var pathCreator = GetPathCreator();
        var path = pathCreator.path;
        var crossSections = GetCrossSections();
        var numVertices = CountVertices(path);

        var vertices = new Vector3[numVertices];
        for (int i = 0; i < path.NumPoints; i++)
        {
            // Get the point on the vertex path
            var point = path.GetPoint(i);

            // Find the time t of the point
            var t = path.GetClosestTimeOnPath(point);

            // Find the cross sections that the point lies between
            var (crossSection1, crossSection2, t2) = GetCrossSections(t);

            // Find the shape of the point using the cross sections
            var shape = ShapeInterpolator.GetShape(crossSection1.GetPoints(), crossSection2.GetPoints(), t2);

            // We need to connect shape to the mesh we already have. Somehow.
        }
    }

    private int CountVertices(VertexPath path)
    {
        var numVertices = 0;
        for (int i = 0; i < path.NumPoints; i++)
        {
            // Get the point on the vertex path
            var point = path.GetPoint(i);

            // Find the time t of the point
            var t = path.GetClosestTimeOnPath(point);

            // Find the cross sections that the point lies between
            var (crossSection1, crossSection2, t2) = GetCrossSections(t);

            // Find the shape of the point using the cross sections
            var shape = ShapeInterpolator.GetShape(crossSection1.GetPoints(), crossSection2.GetPoints(), t2);

            // Add the number of vertices to the total count
            numVertices += shape.Length;
        }

        return numVertices;
    }

    private (CrossSection a, CrossSection b, float t2) GetCrossSections(float t)
    {
        var crossSections = GetCrossSections();

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
            if (crossSection.t < t)
            {
                continue;
            }

            var prevCrossSection = crossSections[i - 1];
            var t2 = Mathf.InverseLerp(prevCrossSection.t, crossSection.t, t);
            return (prevCrossSection, crossSection, t2);
        }

        // The t value is outside the defined cross sections.
        return (crossSections[0], crossSections[0], 0); // todo
    }
}