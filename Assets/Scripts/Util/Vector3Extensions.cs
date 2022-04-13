using UnityEngine;
using PathCreation;
using System.Linq;


namespace Vector3Extension
{
    public static class Vector3ExtensionClass
    {
        public static Vector3[] To3DPoints(this Vector2[] points2D, VertexPath path, float t)
        {
            return points2D.Select(p => p.To3DPoint(path, t)).ToArray();
        }

        public static Vector3 To3DPoint(this Vector2 point2D, VertexPath path, float t)
        {
            var point = (Vector3)point2D;

            var position = path.GetPointAtTime(t);
            var direction = path.GetDirection(t);
            var normal = path.GetNormal(t);
            var rotation = Quaternion.LookRotation(direction, normal);

            point = rotation * point;
            point = position + point;

            return point;
        }
    }
}