using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightScript : MonoBehaviour
{
    public int ID = 1;
    public string IP = "unassigned";
    public int NumStripLeds = 1; //number of unique colors
    public GameObject Light1;
    public Color[] lightColors;
    Light lt1;
    // Start is called before the first frame update
    void Start()
    {
        lightColors = new Color[NumStripLeds];
        lt1 = Light1.GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        lt1.color = lightColors[0];
    }
}
