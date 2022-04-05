using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEditor;
using PathCreation;

[CustomEditor(typeof(OverlapFix))]
public class OverlapEditor : Editor {

    OverlapFix overlap;
    PathCreator creator;
    Extruder extruder;
    int amount = 10;

    public override void OnInspectorGUI () {
        amount = EditorGUILayout.IntField("Number of iterations:", amount);
        //tries to fix overlap with a total of 50 runs of the FixOverlap algorithm else tells user that it failed to fix the overlap.
        if (GUILayout.Button ("Fix Overlap")) {
            for (int i = 0; i < amount; i++) {
                if (!FixOverlap()) {
                    Debug.Log("Overlap is fixed");
                    return;
                }
            }
            Debug.Log("Not all overlapping places could be fixed, either try again or manually adjust the bezier curve.");
        }
        
    }

    void OnEnable() {
        overlap = (OverlapFix) target;
        creator = overlap._pathCreator;
        extruder = overlap._extruder;
    }

    //search for overlapping points and try to fix them
    bool FixOverlap() {
        //First we will search for the intersection line between the planes given by two different cross sections.
        //Create planes using first 3 points of a cross section (possible since the cross sections are 2D inside a 3D space).
        //With the intersection line we check for intersection between this line and all lines in the cross sections
        //to see if they cross both cross sections and thus these two cross sections intersect.
        for (int i = 0; i < extruder.vertexShapes.Length; i++) {
            var shapeI = extruder.vertexShapes[i];
            var pointsI = shapeI.Get3DPoints();
            Plane planeI;
            if (pointsI.Length >= 3) {
                planeI = new Plane(pointsI[0], pointsI[1], pointsI[2]);
            } else {continue;}
            for (int j = i + 1; j < extruder.vertexShapes.Length; j++) {
                
                var shapeJ = extruder.vertexShapes[j];
                var pointsJ = shapeJ.Get3DPoints();
                //If two T values are the same then the cross section is the same so skip over this iteration.
                if (shapeI.GetT() == shapeJ.GetT()) {
                    continue;
                }
                Plane planeJ;
                if (pointsJ.Length >= 3) {
                    planeJ = new Plane(pointsJ[0], pointsJ[1], pointsJ[2]);
                } else {continue;}
                
                Vector3 linePoint;
                Vector3 lineVec;
                planePlaneIntersection(out linePoint, out lineVec, planeI, planeJ, pointsI[0], pointsJ[0]);
                bool intersectI = false;
                bool intersectJ = false;
                //For all points in cross section i we calculate if they intersect with the intersection line of the planes.
                for (int i1 = 0; i1 < pointsI.Length; i1++) {
                    Vector3 lineI;
                    int i2;
                    if (i1 == 0) {
                        i2 = pointsI.Length - 1;
                    } else {
                        i2 = i1 - 1;
                    }
                    lineI = pointsI[i2] - pointsI[i1];
                    Vector3 intersection;
                    if (LineLineIntersection(out intersection, pointsI[i1], lineI, linePoint, lineVec)) {
                        float iSqrMagnitude = lineI.sqrMagnitude;
                        if ((intersection - pointsI[i1]).sqrMagnitude <= iSqrMagnitude  
                            && (intersection - pointsI[i2]).sqrMagnitude <= iSqrMagnitude) 
                            {
                            //intersection is in polygon i
                            intersectI = true;
                        }
                    }
                }
                //For all points in cross section j we calculate if they intersect with the intersection line of the planes.
                for (int j1 = 0; j1 < pointsJ.Length; j1++) {
                    Vector3 lineJ;
                    int j2;
                    if (j1 == 0) {
                        j2 = pointsJ.Length - 1;
                    } else {
                        j2 = j1 - 1;
                    }
                    lineJ = pointsJ[j2] - pointsJ[j1];
                    Vector3 intersection;
                    if (LineLineIntersection(out intersection, pointsJ[j1], lineJ, linePoint, lineVec)) {
                        float jSqrMagnitude = lineJ.sqrMagnitude;
                        if ((intersection - pointsJ[j1]).sqrMagnitude <= jSqrMagnitude  
                            && (intersection - pointsJ[j2]).sqrMagnitude <= jSqrMagnitude) 
                            {
                            //intersection is in polygon j
                            intersectJ = true;
                        }
                    }
                }
                //If the cross sections intersect look at which anchor point of the bezier curve is closest.
                //And adjust bezier curve accordingly
                if (intersectI && intersectJ) {
                    var tPointI = creator.path.GetPointAtTime(shapeI.GetT());
                    var tPointJ = creator.path.GetPointAtTime(shapeJ.GetT());
                    var bezPoint = creator.bezierPath.GetPoint(0);
                    var minDistI = Vector3.Distance(tPointI, bezPoint);;
                    var minDistJ = Vector3.Distance(tPointJ, bezPoint);
                    var anchorI = 0;
                    var anchorJ = 0;
                    for (int t = 1; t < creator.bezierPath.NumAnchorPoints; t++) {
                        bezPoint = creator.bezierPath.GetPoint(3 * t);
                        var distIBez = Vector3.Distance(tPointI, bezPoint);
                        var distJBez = Vector3.Distance(tPointJ, bezPoint);
                        if (distIBez < minDistI) {
                            minDistI = distIBez;
                            anchorI = t * 3;
                        }
                        if (distJBez < minDistJ) {
                            minDistJ = distJBez;
                            anchorJ = t * 3;
                        }
                    }
                    //If the closest anchor point to the two intersecting cross sections is the same.
                    //Then we move that anchor point in line with its neighbour as the turn is to sharp.
                    //Else we move the two anchor points away from each other since the bezier curve is bending in on itself.
                    if (anchorI == anchorJ) {
                        moveInLine(anchorI);
                    } else {
                        moveApart(anchorI, anchorJ);
                    }
                    return true;
                }
            }
        }
        return false;
    }

    //Find the intersection point between two lines.
    //The inputs are two line vectors and points on that line.
    //The output is the point where the lines intersect together with a boolean if they intersect yes or no.
    public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1,
        Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2){

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar, and not parallel
        if( Mathf.Abs(planarFactor) < 0.0001f 
                && crossVec1and2.sqrMagnitude > 0.0001f)
        {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) 
                    / crossVec1and2.sqrMagnitude;
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        }
        else
        {
            intersection = Vector3.zero;
            return false;
        }
    }

    //Find the line of intersection between two planes.
    //The inputs are two game objects which represent the planes.
    //The outputs are a point on the line and a vector which indicates it's direction.
    void planePlaneIntersection(out Vector3 linePoint, out Vector3 lineVec, Plane plane1, Plane plane2, Vector3 plane1point, Vector3 plane2point){
    
        linePoint = Vector3.zero;
        lineVec = Vector3.zero;
    
        //Get the normals of the planes.
        Vector3 plane1Normal = plane1.normal;
        Vector3 plane2Normal = plane2.normal;
    
        //We can get the direction of the line of intersection of the two planes by calculating the
        //cross product of the normals of the two planes. Note that this is just a direction and the line
        //is not fixed in space yet.
        lineVec = Vector3.Cross(plane1Normal, plane2Normal);
    
        //Next is to calculate a point on the line to fix it's position. This is done by finding a vector from
        //the plane2 location, moving parallel to it's plane, and intersecting plane1. To prevent rounding
        //errors, this vector also has to be perpendicular to lineDirection. To get this vector, calculate
        //the cross product of the normal of plane2 and the lineDirection.      
        Vector3 ldir = Vector3.Cross(plane2Normal, lineVec);      
    
        float numerator = Vector3.Dot(plane1Normal, ldir);
    
        //Prevent divide by zero.
        if(Mathf.Abs(numerator) > 0.000001f){
        
            Vector3 plane1ToPlane2 = plane1point - plane2point;
            float t = Vector3.Dot(plane1Normal, plane1ToPlane2) / numerator;
            linePoint = plane2point + t * ldir;
        }
    }

    //Move the anchor point in line with its neighbours.
    //If the anchor point is the first or last point then use the neighbour and the indirect neighbour.
    //Calculate line between the two neighbours and move the anchor point towards this line.
    void moveInLine(int anchorT) {
        Vector3 linePoint1;
        Vector3 linePoint2;
        var anchorPoint = creator.bezierPath.GetPoint(anchorT);
        if (anchorT == 0) {
            linePoint1 = creator.bezierPath.GetPoint(3);
            linePoint2 = creator.bezierPath.GetPoint(6);
        } else if (anchorT == creator.bezierPath.NumPoints - 1) {
            linePoint1 = creator.bezierPath.GetPoint(anchorT - 3);
            linePoint2 = creator.bezierPath.GetPoint(anchorT - 6);
        } else {
            linePoint1 = creator.bezierPath.GetPoint(anchorT - 3);
            linePoint2 = creator.bezierPath.GetPoint(anchorT + 3);
        }
        var goalPoint = linePoint1 + Vector3.Project(anchorPoint - linePoint1, linePoint2 - linePoint1);
        creator.bezierPath.SetPoint(anchorT, Vector3.MoveTowards(anchorPoint, goalPoint, 0.1f));
        creator.bezierPath.AutoSetAllControlPoints();
    }

    //If a anchor point is the first or last point from the bezier curve only move this anchor
    //point away from the other anchor point
    //Else move both points away from each other
    void moveApart(int anchor1, int anchor2) {
        var anchorPoint1 = creator.bezierPath.GetPoint(anchor1);
        var anchorPoint2 = creator.bezierPath.GetPoint(anchor2);
        if (anchor1 == 0) {
            Vector3 move = anchorPoint1 - anchorPoint2;
            creator.bezierPath.SetPoint(anchor1, anchorPoint1 + 0.1f * move.normalized);
        } else if (anchor2 == 0) {
            Vector3 move = anchorPoint2 - anchorPoint1;
            creator.bezierPath.SetPoint(anchor2, anchorPoint2 + 0.1f * move.normalized);
        } else if (anchor1 == creator.bezierPath.NumPoints - 1) {
            Vector3 move = anchorPoint1 - anchorPoint2;
            creator.bezierPath.SetPoint(anchor1, anchorPoint1 + 0.1f * move.normalized);
        } else if (anchor2 == creator.bezierPath.NumPoints - 1) {
            Vector3 move = anchorPoint2 - anchorPoint1;
            creator.bezierPath.SetPoint(anchor2, anchorPoint2 + 0.1f * move.normalized);
        } else {
            Vector3 move = anchorPoint2 - anchorPoint1;
            creator.bezierPath.SetPoint(anchor2, anchorPoint2 + 0.05f * move.normalized);
            creator.bezierPath.SetPoint(anchor1, anchorPoint1 - 0.05f * move.normalized);
        }
        creator.bezierPath.AutoSetAllControlPoints();
    }
}
