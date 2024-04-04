using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class LightController : MonoBehaviour
{
    //State Vars
    String State = "FlashAllColor"; //Allrainbow,FlashAllColor
    public bool faketrigger = false;
    public float faketriggertime = 1.0f;

    //UI Interfaces
    public Text CurrentIPText;

    //ChangeTimeout 
    private float nextActionTime = 0.0f;
    public float minChangePeriod = 0.3f;


    private int state = 1;
    private UdpClient udpClient;

    //Light Settings
    public GameObject[] lights;

    //Network settings
    public int sendTimeout = 20;
    public int reciveTimeout = 20;
    public int port = 42069;
    public byte subnet = 1;
    public bool scanning = false;
    public byte currentscan = 1;

    //Grapher Refs
    public Grapher RawDBGraph;

    //Avg values
    float AvgRawDB = 0.0f;
    int AvgRawDBCount = 1;

    //Async update lights, set at 50Hz
    private float nextUpdateTime = 0.0f;
    public float UpdatePeriod = 0.02f;
    // Start is called before the first frame update
    void Start()
    {
        lights = GameObject.FindGameObjectsWithTag("Light");
        Debug.Log("number of light scene has: "+lights.Length.ToString());
        if(faketrigger){
            InvokeRepeating("ChangeLights", 0.0f, faketriggertime);
        }
        //TODO, load ips from file
        //TODO, check current ip's still exsist on the network, if not change to NONE for ip feild
    }

    void FixedUpdate(){ //update with physics engine, by default locked to 50hz     
        GetAvgSamples();

        if (scanning) {
            EveryFrameScan();
        }else{
            UpdateLights();
        }
        AvgSamples();
    }

    public void ChangeLights(){ //actually change the lights (Color ect), called on fixedupdate (every 20 ms)
        if (Time.time > nextActionTime ) { //check to make sure this issn't triggered more than once every minChangePeriod
            nextActionTime += minChangePeriod;
            if (State == "FlashAllColor"){
                foreach (var light in lights) {
                    LightScript CurrentLight = light.GetComponent<LightScript>();
                    //Color setColor = Color.HSVToRGB(UnityEngine.Random.Range(1, 255),1,0.5f); //set to random color
                    Color setColor = Color.HSVToRGB(UnityEngine.Random.Range(0.0f, 1.0f),1,1); //set to random color
                    CurrentLight.lightColors[0] = setColor;
                }  
            }else if(State == "Allrainbow"){
                foreach (var light in lights) {
                    LightScript CurrentLight = light.GetComponent<LightScript>();
                    Color.RGBToHSV(CurrentLight.lightColors[0], out float H, out float S, out float V);
                    float hugh = H;
                    hugh = hugh + 0.001f;
                    Color setColor = Color.HSVToRGB(hugh,1,0.5f);
                    CurrentLight.lightColors[0] = setColor;
                }
            }
        }
    }

    void GetAvgSamples(){ //called at 50hz, takes avg of db values taken in between 50hz samples
        AvgRawDB = AvgRawDB/AvgRawDBCount;
        AvgRawDBCount = 1;
    }

    void AvgSamples(){ //called as many times as update
        AvgRawDB = AvgRawDB + MicInput.MicLoudnessinDecibels;
        AvgRawDBCount++;
    }

    void Update(){

        //RawDBGraph.AddValue(MicInput.MicLoudnessinDecibels+80);

        //send new packet every frame
        /*
        if(MicInput.MicLoudness > 0.001){
            Debug.Log("Raw,DB: "+MicInput.MicLoudness.ToString()+","+MicInput.MicLoudnessinDecibels.ToString());
        }
        */
    }

    void UpdateLights(){ //update lights via network
       
        foreach (var light in lights) {

            LightScript CurrentLight = light.GetComponent<LightScript>();
            //Debug.Log("Currentlight IP: "+ CurrentLight.IP +" ID: "+ CurrentLight.ID);
            if (CurrentLight.IP != "NONE") { //check to make sure ip has been assigned

                byte r,g,b;
                r = (byte)((CurrentLight.lightColors[0].r)*255);
                g = (byte)((CurrentLight.lightColors[0].g)*255);
                b = (byte)((CurrentLight.lightColors[0].b)*255);
                byte[] Packet = new byte[] {0x02,r,g,b};
                //Debug.Log("Sending update packet to light [ID,IP]: [" + CurrentLight.IP + "," + CurrentLight.IP + "]");
                SendPacket(Packet,new UdpClient(CurrentLight.IP, port));
            }
            
        }

        //SendPacket(Packet,new UdpClient("192.168.1.147", port));
        //SendPacket(Packet,new UdpClient("192.168.10.179", port));
        //SendPacket(Packet,new UdpClient("192.168.10.190", port));
    }

    void SendPacket(Byte[] sendBytes, UdpClient Client){
        try{
            Client.Send(sendBytes, sendBytes.Length);
        }
        catch ( Exception e ){
            Debug.Log( e.ToString());
        }
    }

    void EveryFrameScan(){
        int addr = currentscan;
        String IP = "192.168."+subnet.ToString()+"."+addr.ToString();
        //Debug.Log("Trying To Fetch ID from Client at: "+IP);
        //Debug.Log("Scanning Client");
        //CurrentIPText.text = "Current IP: "+IP;
        //CurrentIPText.text = IP;

        UdpClient CurrentClient = new UdpClient(IP, port);
        int id = GetID(CurrentClient);
        if(id != 0){
            foreach (var light in lights) {
            LightScript CurrentLight = light.GetComponent<LightScript>();
            if(CurrentLight.ID == id){
                CurrentLight.IP = IP;
            }         
            Debug.Log("Found device ID:"+id.ToString()+" at ip:"+IP.ToString());
            }
        }
        if(currentscan == 255){
            scanning = false;
            currentscan = 1;
            return;
        }      
        currentscan++;
    }

    public void ScanNetForLights(){
        if(!scanning){
            Debug.Log("Scanning Entire Network");
            scanning = true;
            currentscan = 1;
         }
    }

    int GetID(UdpClient Client){
        Client.Client.SendTimeout = sendTimeout;
        Client.Client.ReceiveTimeout = reciveTimeout;
        byte[] Packet = new byte[] {0x01}; //command to get ID
        SendPacket(Packet,Client);  
        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        try {
            // Blocks until a message returns on this socket from a remote host.
            Byte[] receiveBytes = Client.Receive(ref RemoteIpEndPoint);
            int ID = receiveBytes[0];
            //Debug.Log(receiveBytes);
            Client.Close();
            return ID;
        }catch (Exception) { //didn't recive reply, must not be right device
            //Debug.Log(e.ToString()); //do log o
            
            Client.Close();
            return 0;
        }
    }
}

/*
    // Update is called once per frame
    void Update()
    {     
         if (Time.time > nextUpdateTime ) {
            UpdateLights();
            nextUpdateTime += UpdatePeriod;
        }
    }
*/