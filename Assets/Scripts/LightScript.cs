using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightScript : MonoBehaviour
{
    public enum LightType { StripSingleColor, StripMultiColor, SingleBrightLight, Strobe };

    public LightType CurrentType = LightType.StripMultiColor;

    public GameObject[] LEDS;

    public int ID = 1;
    public string IP = null;
    public int NumStripLeds = 1; //number of unique colors
    public GameObject Light1;
    public Color[] lightColors;
    Light lt1;
    // Start is called before the first frame update
    void Start()
    {
        lt1 = Light1.GetComponent<Light>();
        //LEDS = GameObject.FindGameObjectsWithTag("LED");
        NumStripLeds = LEDS.Length;
        lightColors = new Color[NumStripLeds];
    }

    // Update is called once per frame
    void Update()
    {
        lt1.color = lightColors[0];
        for(int i = 0; i < LEDS.Length; i++) {
            //Debug.Log("Changing color of led: "+ LEDS[i].name);
            var LEDRenderer = LEDS[i].GetComponent<Renderer>();
            LEDRenderer.material.color = lightColors[i];
        }
    }
}
