using System;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;
using System.Linq;


namespace Vector3Extension
{
    public static class Vector3ExtensionClass
    {
        public static Vector3[] To3DPoints(this Vector2[] points3D, VertexPath path, float t)
        {
            return points3D.Select(p => p.To3DPoint(path, t)).ToArray();
        }

        public static Vector3 To3DPoint(this Vector2 point2D, VertexPath path, float t)
        {
            var point = (Vector3)point2D;

            var position = path.GetPointAtTime(t, EndOfPathInstruction.Stop);
            var direction = path.GetDirection(t, EndOfPathInstruction.Stop);
            var rotation = Quaternion.FromToRotation(Vector3.forward, direction);

            point = rotation * point;
            point = position + point;

            return point;
        }
    }
}