using UnityEngine;
using PathCreation;
using System.Linq;


namespace Vector3Extension
{
    public static class Vector3ExtensionClass
    {
        public static Vector2[] To3DPoints(this Vector3[] points3D, Vector3 pathDirection)
        {
            return points3D.Select(p => p.To2DPoint(pathDirection)).ToArray();
        }

        public static Vector2 To2DPoint(this Vector3 point3D, Vector3 pathDirection)
        {
            return (Vector2)Vector3.ProjectOnPlane(point3D, pathDirection);
        }

        public static Vector3[] To3DPoints(this Vector2[] points2D, VertexPath path, float t)
        {
            return points2D.Select(p => p.To3DPoint(path, t)).ToArray();
        }

        public static Vector3 To3DPoint(this Vector2 point2D, VertexPath path, float t)
        {
            var point = (Vector3)point2D;

            var position = path.GetPointAtTime(t);
            var direction = path.GetDirection(t);
            var rotation = Quaternion.FromToRotation(Vector3.forward, direction);

            point = rotation * point;
            point = position + point;

            return point;
        }
    }
}