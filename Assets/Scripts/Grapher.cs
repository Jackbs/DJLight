using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grapher : MonoBehaviour
{
    public int graphValues = 400; //4 seconds at 50 samples/second
    public GameObject[] Boxes;
    public float[] Values;
    public Color GraphColor;
    public float minvalue = -100.0f;
    public float maxvalue = 200.0f;

    public float xPosScale = 0.25f;

    public float yoffset = 0.0f;

    
    public LineRenderer lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        Values = new float[graphValues];

        //lineRenderer = new GameObject("Line").AddComponent<LineRenderer>();
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;
        lineRenderer.startWidth = 0.8f;
        lineRenderer.endWidth = 0.8f;
        lineRenderer.positionCount = graphValues;
        lineRenderer.transform.parent = transform;
        lineRenderer.useWorldSpace = true;    

        /*
        Boxes = new GameObject[graphValues];
        
        for(int i = 0;i<graphValues;i++){
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Red_cube";
            cube.GetComponent<Renderer>().material.color = GraphColor;
            cube.transform.parent = transform;
            cube.transform.localPosition = new Vector3(-i, 0, 0);
            Boxes[i] = cube;
        }
        Values[40] = 10;
        Values[120] = 12;
        */
    }

    // Update is called once per frame
    void Update()
    {
        for(int i = 0;i<graphValues;i++){
            Vector3 currentPos = new Vector3(-i*xPosScale, Values[i]+yoffset, 0);
            //Boxes[i].transform.localPosition = currentPos;
            lineRenderer.SetPosition(i, currentPos);
        }
    }

    public void AddValue(float value){
        if(float.IsNaN(value)){value = 0.0f; }
        if(float.IsInfinity(value)){value = 0.0f; }
        Mathf.Clamp(value, minvalue, maxvalue);
        Values[graphValues-1] = value;
        for(int i = 0;i<graphValues-1;i++){
            Values[i] = Values[i+1];
        }
    }
}
