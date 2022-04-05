using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;
using static Extruder;
using static ShapeData;
using static CrossSection;

[ExecuteInEditMode]
[RequireComponent(typeof(PathCreator))]
public class OverlapFix : MonoBehaviour
{

    public PathCreator _pathCreator;
    public Extruder _extruder;
   


    private void Awake()
    {
        _pathCreator = GetComponentInParent<PathCreator>();
        _extruder = GetComponent<Extruder>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Assert(_pathCreator != null, "pathCreator null");
    }

    void OnDrawGizmos() {

    }
}
