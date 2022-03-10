using UnityEngine;
using PathCreation;
using System.Linq;
using MinByExtension;
using Vector3Extension;


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
        // CreateCombinedMesh();
    }

    private void CreateCombinedMesh()
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
            Gizmos.DrawSphere(point, 0.05f);

            // Find the time t of the point on the path
            var t = path.GetClosestTimeOnPath(point);

            // Find the cross sections that the point lies between using t
            var (crossSection1, crossSection2, t2) = GetCrossSections(t);

            // Find the shape of the point using the cross sections using t2
            var middleShape = ShapeInterpolator.GetShape(crossSection1.Get2DPoints(), crossSection2.Get2DPoints(), t2);

            // Move the shape to the correct position on the path
            // middleShape = middleShape.Select(v => v + point).ToArray();
            foreach (var vertex in middleShape)
            {
                // var p = vertex.To3DPoint(path, t);
                // Gizmos.DrawCube(p, Vector3.one * 0.1f);
            }
            // Debug.Log($"t: {t2}, c1: {crossSection1.GetPoints()[0]}, c2: {crossSection2.GetPoints()[0]}, middle: {middleShape[0]}");

            // We need to connect shape to the mesh we already have. Somehow.
            // Find the shape with the most vertices. From has more vertices
            // var from = crossSection1.Get3DPoints();
            // var to = middleShape;
            // if (middleShape.Length > crossSection1.Get3DPoints().Length)
            // {
            //     from = middleShape;
            //     to = crossSection1.Get3DPoints();
            // }

            // var edges = new (Vector3, Vector3)[from.Length];
            // for (var j = 0; j < from.Length; j++)
            // {
            //     var p = from[j];
            //     var cP = GetClosestPoint(to, p);
            //     // we need to connect p with closestPoint
            //     edges[j] = (p, cP);
            //     // Gizmos.DrawLine(p, cP);
            // }
            // // Debug.Log($"numedges: {edges.Length}, from: {from.Length}, to: {to.Length}");
        }
    }

    private void OnDrawGizmos()
    {

        CreateCombinedMesh();
    }

    private Vector3 GetClosestPoint(Vector3[] points, Vector3 reference)
    {
        if (points.Length == 0)
        {
            return Vector3.zero; //TODO
        }

        var closestPoint = points.MinBy(p => Vector3.Dot(p, reference));
        return closestPoint;
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
            var shape = ShapeInterpolator.GetShape(crossSection1.Get2DPoints(), crossSection2.Get2DPoints(), t2);

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