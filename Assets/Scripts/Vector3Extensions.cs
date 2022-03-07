using System;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;


namespace Vector3Extension
{
    public static class Vector3ExtensionClass
    {
        public static Vector2 To2DPoint(this Vector3 point3D, Vector3 pathDirection)
        {
            return (Vector2)Vector3.ProjectOnPlane(point3D, pathDirection);
        }

        public static Vector3 To3DPoint(this Vector2 point2D, VertexPath path, float t)
        {
            var pathDirection = path.GetDirection(t);
            var positionOnPath = path.GetPointAtTime(t, EndOfPathInstruction.Stop);
            var point = point2D;

            point = Quaternion.LookRotation(pathDirection) * point;
            point = (Vector3)point2D + positionOnPath;
            return point;
        }

        // public static Vector3 ToPathPoint(this Vector3 point, Vector3 pathDirection)
        // {

        // }
    }
}