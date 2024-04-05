using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Random = UnityEngine.Random;

public class LightController : MonoBehaviour
{
    public enum SysStates { FlashAllColor, SimpleRainbowAll, Bla1, Bla2 };

    public SysStates State = SysStates.FlashAllColor;
    //State Vars

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

    public float idleRainBowSpeed = 0.002f;
    //COlOR referenc
    Color GlobalRefColor = Color.red;

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
            NetUpdateLights();
            FixedUpdateLights();
        }
        GetAvgSamples();
    }



    void Update() {
        AvgSamples();
    }

    Color ChangeHughBy(Color thisColor, float offset) {
        Color.RGBToHSV(thisColor, out float H, out float S, out float V);
        float hugh = H;
        hugh = hugh + offset;
        return Color.HSVToRGB(hugh, 1.0f, 1.0f);
    }

    void FixedUpdateLights(){
        switch(State) {
            case SysStates.SimpleRainbowAll:
                foreach (var light in lights) {
                    LightScript CurrentLight = light.GetComponent<LightScript>();
                    GlobalRefColor = ChangeHughBy(GlobalRefColor, idleRainBowSpeed);

                    if (CurrentLight.CurrentType == LightScript.LightType.StripSingleColor) {
                        CurrentLight.lightColors[0] = GlobalRefColor;
                    } else if (CurrentLight.CurrentType == LightScript.LightType.StripMultiColor) {
                        CurrentLight.lightColors[0] = GlobalRefColor;
                        for (int i = 0; i < CurrentLight.lightColors.Length-1; i++) {
                            CurrentLight.lightColors[i+1] = ChangeHughBy(CurrentLight.lightColors[i],0.01f);
                        }
                    } else {
                        Debug.Log("Light is unknowen type");
                    }
                }
                break;


            default:
                break;
        }
    }

    public void ChangeLights(){ //actually change the lights (Color ect), called on fixedupdate (every 20 ms)
        if (Time.time > nextActionTime ) { //check to make sure this issn't triggered more than once every minChangePeriod
            nextActionTime += minChangePeriod;
            if (State == SysStates.FlashAllColor) {
                foreach (var light in lights) {
                    LightScript CurrentLight = light.GetComponent<LightScript>();
                    //Color setColor = Color.HSVToRGB(UnityEngine.Random.Range(1, 255),1,0.5f); //set to random color
                    Color setColor = Color.HSVToRGB(UnityEngine.Random.Range(0.0f, 1.0f),1,1); //set to random color
                    if(CurrentLight.CurrentType == LightScript.LightType.StripSingleColor) {
                        CurrentLight.lightColors[0] = setColor;
                    }else if(CurrentLight.CurrentType == LightScript.LightType.StripMultiColor) {
                        for (int i = 0; i < CurrentLight.lightColors.Length; i++) {
                            CurrentLight.lightColors[i] = setColor;
                        }
                    }else{
                        Debug.Log("Light is unknowen type");
                    }
                }  
            }else if(State == SysStates.SimpleRainbowAll) { //do something during beat detected
                GlobalRefColor = ChangeHughBy(GlobalRefColor, Random.Range(0.4f, 0.8f));
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

    void NetUpdateLights(){ //update lights via network
        foreach (var light in lights) {
            LightScript CurrentLight = light.GetComponent<LightScript>();
            if (CurrentLight.IP != null) { //check to make sure ip has been assigned
                byte[] Packet = null;
                if (CurrentLight.CurrentType == LightScript.LightType.StripSingleColor) {
                    byte r, g, b;
                    r = (byte)((CurrentLight.lightColors[0].r) * 255);
                    g = (byte)((CurrentLight.lightColors[0].g) * 255);
                    b = (byte)((CurrentLight.lightColors[0].b) * 255);
                    Packet = new byte[] { 0x02, r, g, b };
                } else if (CurrentLight.CurrentType == LightScript.LightType.StripMultiColor) {
                    int NumLEDSend = 300;
                    Packet = new byte[1 + (3 * NumLEDSend)]; //assign size of buffer depending on what we are sending
                    Packet[0] = 0x04;
                    for (int i = 0; i < NumLEDSend; i++) {
                        byte r, g, b;

                        r = (byte)((CurrentLight.lightColors[i].r) * 255);
                        g = (byte)((CurrentLight.lightColors[i].g) * 255);
                        b = (byte)((CurrentLight.lightColors[i].b) * 255);

                        Packet[(i * 3) + 1] = r;
                        Packet[(i * 3) + 2] = g;
                        Packet[(i * 3) + 3] = b;
                    }
                } else {
                    Debug.LogError("NETUPDATE Light is unknowen type");
                    
                }

                //Debug.Log("Sending update packet to light [ID,IP]: [" + CurrentLight.IP + "," + CurrentLight.IP + "]");
                SendPacket(Packet,new UdpClient(CurrentLight.IP, port));
            }
            
        }
        /*
        int NumLEDSend = 3;
                    Packet = new byte[1 + (3 * NumLEDSend)]; //assign size of buffer depending on what we are sending
                    for (int i = 0; i < NumLEDSend; i++) {
                        byte r, g, b;
                        
                        r = (byte)((CurrentLight.lightColors[i].r) * 255);
                        g = (byte)((CurrentLight.lightColors[i].g) * 255);
                        b = (byte)((CurrentLight.lightColors[i].b) * 255);

                        Packet[(i * 3) + 1] = r;
                        Packet[(i * 3) + 2] = g;
                        Packet[(i * 3) + 3] = b;
                    } 
         */

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