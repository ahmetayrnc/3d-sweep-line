using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapeMorphing : MonoBehaviour
{

    List<Vector3> shape1;
    List<Vector3> shape2;
    int t;

    //'rectangle': [(0,0,0),(0,1,0),(1,1,0),(1,0,0),(0,0,0)],
    //'triangle': [(0,0,0),(0.5,1,0),(1,0,0),(0,0,0)],

    // Start is called before the first frame update
    void Start()
    {
        shape1 = new List<Vector3>();
        shape2 = new List<Vector3>();
        shape1.Add(new Vector3(0,0,0));
        shape1.Add(new Vector3(0,1,0));
        shape1.Add(new Vector3(1,1,0));
        shape1.Add(new Vector3(1,0,0));
        shape1.Add(new Vector3(0,0,0));
        shape2.Add(new Vector3(0,0,0));
        shape2.Add(new Vector3((float)0.5,1,0));
        shape2.Add(new Vector3(1,0,0));
        shape2.Add(new Vector3(0,0,0));
        t = 5;
        List<Vector3> result = MorphShape(shape1, shape2, t);
        foreach (Vector3 i in result) {
            Debug.Log(i);
            Debug.Log(i.x);
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public List<Vector3> MorphShape(List<Vector3> firstShape, List<Vector3> secondShape, int t)
    {

        // Finds the shape with less no. of vertices and repeatedly adds 
        // the last vertex of that shape into its coordinates to match 
        // the no. of vertices in both shapes (like a virtual vertex)
        if (firstShape.Count > secondShape.Count) {
            Vector3 lastVertex = secondShape[secondShape.Count - 1];
            for (int i = 0; i < firstShape.Count - secondShape.Count; i++) {
                secondShape.Add(lastVertex);
            }
        }
        else if (firstShape.Count < secondShape.Count) {
            Vector3 lastVertex = firstShape[firstShape.Count - 1];
            for (int i = 0; i < secondShape.Count - firstShape.Count; i++) {
                firstShape.Add(lastVertex);
            }
        }

        int nrOfVertices = firstShape.Count;
        List<Vector3> vertices = new List<Vector3>();

        // Morphing Calculations
        // Calculate the new interim coordinates of each node
        // Value 10 can be changed according to our need
        // 3rd dimension is added (vertexZ)

        for (int i = 0; i < nrOfVertices; i++) {
            float newX = firstShape[i].x + (secondShape[i].x - firstShape[i].x) * t/10;
            float newY = firstShape[i].y + (secondShape[i].y - firstShape[i].y) * t/10;
            float newZ = firstShape[i].z + (secondShape[i].z - firstShape[i].z) * t/10;
            vertices.Add(new Vector3(newX,newY,newZ));
        }
        return vertices;
    }
}
