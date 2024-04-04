using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MicInput : MonoBehaviour
{
    #region SingleTon
 
    public static MicInput Inctance { set; get; }

    #endregion

    public Text MicInputText;

    public static float MicLoudness;
    public static float MicLoudnessinDecibels;

    const int windowSize = 256;

    private string _device;

    public AudioSource source;

    public float avgvals = 3.0f;
    public float gain = 16.0f;
    public int FFT_sampleWindow = windowSize;

    //FFT stuff
    public Grapher BassValueGraph;
    public Grapher FirstIndex;
    public Grapher SecondIndex;
    public Grapher ThirdIndex;

    public LightController lightController;
 
    //mic initialization
    public void InitMic()
    {
        if (_device == null)
        {
            _device = Microphone.devices[0]; //SET VOICEMEETER AS DEFAULT MIC AND IT WORKS
        }
        //_clipRecord = Microphone.Start(_device, true, 999, 44100); //44100 //changed for LPF
        //source.clip = Microphone.Start(_device, true, 999, 44100); //44100 //changed for LPF
        source.clip = Microphone.Start(_device, true, 999, 44100); //44100 //changed for LPF
        source.loop = true; 
        source.Play();
        _isInitialized = true;
    }
 
    public void StopMicrophone()
    {
        Microphone.End(_device);
        _isInitialized = false;
    }
 
    AudioClip _clipRecord;
    AudioClip _recordedClip;
    int _sampleWindow = 128;
 
    //get data from microphone into audioclip
    float MicrophoneLevelMax()
    {
        float levelMax = 0;
        float[] waveData = new float[_sampleWindow];
        int micPosition = Microphone.GetPosition(null) - (_sampleWindow + 1); // null means the first microphone
        if (micPosition < 0) return 0;
        //_clipRecord.GetData(waveData, micPosition);
        source.clip.GetData(waveData, micPosition);
        //GetSpect(waveData);
        for (int i = 0; i < _sampleWindow; i++)
        {
            float wavePeak = waveData[i] * waveData[i];
            if (levelMax < wavePeak)
            {
                levelMax = wavePeak;
            }
        }
        return levelMax;
    }

    void FixedUpdate(){


        float[] waveData = new float[FFT_sampleWindow];
        int micPosition = Microphone.GetPosition(null) - (FFT_sampleWindow + 1); // null means the first microphone
        if (micPosition < 0){
            //0
        }else{
            source.clip.GetData(waveData, micPosition);
            if (isBeatEnergy(waveData, waveData))
                lightController.ChangeLights();
            GetSpect(waveData);
        }
        //_clipRecord.GetData(waveData, micPosition);
    }

    private const float MIN_BEAT_SEPARATION = 0.1f; // Minimum beat separation in time
    private const int MAX_HISTORY = 500; // Allocation buffer. Must be greater than MAX_FRQSEC*LINEAR_OCTAVE_DIVISIONS*HISTORY_LENGTH

    private int sampleRange = windowSize;
    private int numHistory = 0;
    private const int HISTORY_LENGTH = 43; // Real number of history buffer per frequency.
    int historyLength = HISTORY_LENGTH;
    int circularHistory = 0;
    float tIni;// = Time.time;
    float[] energyHistory = new float[MAX_HISTORY];
    float[] mediasHistory = new float[MAX_HISTORY];
    bool isBeatEnergy(float[] frames0, float[] frames1)
    {
        float level = 0f;
        for (int i = 0; i < sampleRange; i++)
        {
            // frame0, frame1 corresponding to left, right channel
            // level refers to the instant energy
            //level += (frames0[i] * frames0[i]) + (frames1[i] * frames1[i]);
            level += ((float)Math.Pow(frames0[i], 2)) + ((float)Math.Pow(frames1[i], 2));
        }

        level /= (float)sampleRange;
        float instant = Mathf.Sqrt(level) * 100f; // instant energy level, use for comparison later

        // ------- Local Average History Energy -------

        float E = 0f;
        for (int i = 0; i < numHistory; i++)
        {
            E += energyHistory[i];
        }

        if (numHistory > 0)
        {
            E /= (float)numHistory;
        }

        // ------- Compute Variance of the energies in E -------

        float V = 0f;
        for (int i = 0; i < numHistory; i++)
        {
            V += (energyHistory[i] - E) * (energyHistory[i] - E);
        }

        if (numHistory > 0)
        {
            V /= (float)numHistory;
        }

        // C is a constant which will determine the sensibility of the algorithm to beats
        float C = (-0.0025714f * V) + 1.5142857f;
        float diff = (float)Mathf.Max(instant - C * E, 0f);

        // ------- 

        float dAvg = 0f;
        int num = 0;
        for (int i = 0; i < numHistory; i++)
        {
            if (mediasHistory[i] > 0)
            {
                dAvg += mediasHistory[i];
                num++;
            }
        }

        if (num > 0)
        {
            dAvg /= (float)num;
        }

        float diff2 = (float)Mathf.Max(diff - dAvg, 0f);

        // ------- comparison to determine beat detection -------

        bool detected;
        if (Time.time - tIni < MIN_BEAT_SEPARATION)
        {
            detected = false;
        }
        else if (diff2 > 0.0 && instant > 2.0)
        {
            detected = true;
            tIni = Time.time;
        }
        else
        {
            detected = false;
        }

        numHistory = (numHistory < historyLength) ? numHistory + 1 : numHistory;

        energyHistory[circularHistory] = instant;
        mediasHistory[circularHistory] = diff;

        circularHistory++;
        circularHistory %= historyLength;

        return detected;
    }


    void GetSpect(float[] spectrum){ //Weird but kinda works?? see https://docs.unity3d.com/ScriptReference/AudioSource.GetSpectrumData.html

        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);
        

        float[] Metrics = new float[4];
        //for a data side of 512, data point number 10 peaked at arround 410Hz 40hz per point, 
        float BassValue = 0.0f;
        float FirstIndexValue = 0.0f;
        float SecondIndexValue = 0.0f;
        float ThirdIndexValue = 0.0f;
        for (int i = 1; i < spectrum.Length - 1; i++) //INDEX 1 NOISY AF
        {
            float value =  Mathf.Abs(Mathf.Log(spectrum[i - 1]) + 10);
            if(i == 3){
                FirstIndexValue = value;
            }else if(i == 2){
                SecondIndexValue = value;
            }else if(i == 4){
                ThirdIndexValue = value;
            }
            /* DEBUG
            Debug.DrawLine(new Vector3(i - 1, spectrum[i] + 10, 0), new Vector3(i, spectrum[i + 1] + 10, 0), Color.red);
            Debug.DrawLine(new Vector3(i - 1, Mathf.Log(spectrum[i - 1]) + 10, 2), new Vector3(i, Mathf.Log(spectrum[i]) + 10, 2), Color.cyan);
            Debug.DrawLine(new Vector3(Mathf.Log(i - 1), spectrum[i - 1] - 10, 1), new Vector3(Mathf.Log(i), spectrum[i] - 10, 1), Color.green);
            Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3), new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i]), 3), Color.blue);
             */
        }

        BassValue = SecondIndexValue + ThirdIndexValue;

        /*
        Metrics[0] = BassValue;
        Metrics[1] = FirstIndexValue;
        Metrics[2] = SecondIndexValue;
        Metrics[3] = ThirdIndexValue;
        */
        


        /*
        Metrics[0] = ((Metrics[0]*(avgvals-1))+(Metrics[0] + BassValue))/avgvals;
        Metrics[1] = ((Metrics[1]*(avgvals-1))+(Metrics[1] + FirstIndexValue))/avgvals;
        Metrics[2] = ((Metrics[2]*(avgvals-1))+(Metrics[2] + SecondIndexValue))/avgvals;
        Metrics[3] = ((Metrics[3]*(avgvals-1))+(Metrics[3] + ThirdIndexValue))/avgvals;

       for (int i = 0; i < Metrics.Length; i++){
            Metrics[i] = Metrics[i]*0.8f;
       }
        */
       

        
        Metrics[0] = (((Metrics[0]*(avgvals-1))+(Mathf.Pow(BassValue,1.5f)))/avgvals);
        Metrics[1] = (((Metrics[1]*(avgvals-1))+(Mathf.Pow(FirstIndexValue,1.5f)))/avgvals);
        Metrics[2] = (((Metrics[2]*(avgvals-1))+(Mathf.Pow(SecondIndexValue,1.5f)))/avgvals);
        Metrics[3] = (((Metrics[3]*(avgvals-1))+(Mathf.Pow(ThirdIndexValue,1.5f)))/avgvals);
        
        for (int i = 0; i < Metrics.Length; i++){
            Metrics[i] = Metrics[i];
       }

        //Metrics[0]; 
        //IGNORED AS GRAPH OBJ IS NOT THERE
        //BassValueGraph.AddValue(Metrics[0]*gain);
        //FirstIndex.AddValue(Metrics[1]*gain);
        //SecondIndex.AddValue(Metrics[2]*gain);
        //ThirdIndex.AddValue(Metrics[3]*gain);
    }
    
 
    //get data from microphone into audioclip
    float MicrophoneLevelMaxDecibels()
    {
        float db = 20 * Mathf.Log10(Mathf.Abs(MicLoudness));
        return db;
    }

    void Update()
    {
        MicLoudness = MicrophoneLevelMax();
        MicLoudnessinDecibels = MicrophoneLevelMaxDecibels();
        if(float.IsNaN(MicLoudness)){MicLoudness = 0.0f; }
        if(float.IsInfinity(MicLoudness)){MicLoudness = 0.0f; }
        if(float.IsNaN(MicLoudnessinDecibels)){MicLoudnessinDecibels = 0.0f; }
        if(float.IsInfinity(MicLoudnessinDecibels)){MicLoudnessinDecibels = -100.0f; }
    }
 
    bool _isInitialized;
    // start mic when program starts

    void Start(){
        tIni = Time.time;
        InitMic(); 
        _isInitialized = true;
        Inctance = this;

        foreach (var device in Microphone.devices)
        {
            Microphone.GetDeviceCaps(device, out int minFreq, out int maxFreq);
            //Debug.Log("MicName,minF,maxF): " + device+","+minFreq.ToString()+","+maxFreq.ToString());
        }
        Debug.Log("Using Microphone Device: "+_device);
        MicInputText.text = _device;
    }
 
    //stop mic when loading a new level or quit application NO JACK WANT ACTIVE ALL TIME
    /*
    void OnDisable(){
        StopMicrophone();
    }
 
    void OnDestroy(){
        StopMicrophone();
    }

    // make sure the mic gets started & stopped when application gets focused
    void OnApplicationFocus(bool focus)
    {
        if (focus){
            //Debug.Log("Focus");
 
            if (!_isInitialized)
            {
                //Debug.Log("Init Mic");
                InitMic();
            }
        }
        if (!focus){
            StopMicrophone();
        }
    }
    */
}




 
    /*
    public float FloatLinearOfClip(AudioClip clip)
    {
        StopMicrophone();
 
        _recordedClip = clip;
 
        float levelMax = 0;
        float[] waveData = new float[_recordedClip.samples];
 
        _recordedClip.GetData(waveData, 0);
        // Getting a peak on the last 128 samples
        for (int i = 0; i < _recordedClip.samples; i++)
        {
            float wavePeak = waveData[i] * waveData[i];
            if (levelMax < wavePeak)
            {
                levelMax = wavePeak;
            }
        }
        return levelMax;
    }
 
    public float DecibelsOfClip(AudioClip clip)
    {
        StopMicrophone();
 
        _recordedClip = clip;
 
        float levelMax = 0;
        float[] waveData = new float[_recordedClip.samples];
 
        _recordedClip.GetData(waveData, 0);
        // Getting a peak on the last 128 samples
        for (int i = 0; i < _recordedClip.samples; i++)
        {
            float wavePeak = waveData[i] * waveData[i];
            if (levelMax < wavePeak)
            {
                levelMax = wavePeak;
            }
        }
        float db = 20 * Mathf.Log10(Mathf.Abs(levelMax));
        return db;
    }
    */
 
 