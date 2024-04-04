using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicInputFilter : MonoBehaviour
{
    private float targetTime = 0.0f;
// Start is called before the first frame update
public AudioSource source;
private const int SAMPLECOUNT = 1024;   // Sample Count.
private const float REFVALUE = 0.1f;    // RMS value for 0 dB.
private const float THRESHOLD = 0.02f;  // Minimum amplitude to extract pitch (recieve anything)
private const float ALPHA = 0.09f;      // The alpha for the low pass filter I don't really understand this

public int recordedLength ;    // How many previous frames of sound are analyzed.
public int requiedBlowTime;    // How long a blow must last to be classified as a blow (and not a sigh for instance).
public int clamp;            // Used to clamp dB (I don't really understand this either).

private float rmsValue;            // Volume in RMS
private float dbValue;             // Volume in DB
private float pitchValue;          // Pitch - Hz (is this frequency?)
private int blowingTime;           // How long each blow has lasted

private float lowPassResults;      // Low Pass Filter result
private float peakPowerForChannel; //

private GameObject[] cubes; // This is cubes array to show spectrum
public float SpectrumRefreshTime; // refresh rate to show cubes
private float lastUpdate = 0;
public float scaleFactor = 10000;
private float[] samples;           // Samples
private float[] spectrum = new float[1024];          // Spectrum
private List<float> dbValues;      // Used to average recent volume.
private List<float> pitchValues;   // Used to average recent pitch.

void Start()
{
    cubes = new GameObject[1024];
    samples = new float[SAMPLECOUNT];
    spectrum = new float[SAMPLECOUNT];
    dbValues = new List<float>();
    pitchValues = new List<float>();

    StartMicListener();

    foreach (var device in Microphone.devices) 
    {
        Debug.Log("Micro Connecté " + device);
    }   
}

private void StartMicListener() /// Starts the Mic, and plays the audio back in (near) real-time.
{
    source.clip = Microphone.Start(Microphone.devices[0], true, 999, AudioSettings.outputSampleRate);
    // HACK - Forces the function to wait until the microphone has started, before moving onto the play function.
    while (!(Microphone.GetPosition(Microphone.devices[0]) > 0))
    {
    }
    source.loop = true; 
    source.Play();
}

// Update is called once per frame
[System.Obsolete]
void Update()
{
    targetTime += Time.deltaTime;
    if (targetTime <= 10.0f)
    { gameStart(); }
    // If the audio has stopped playing, this will restart the mic play the clip.
    if (!source.isPlaying)
    {
        StartMicListener();
    }
    gameStart();
    // Gets volume and pitch values
    AnalyzeSound();
    // Runs a series of algorithms to decide whether a blow is occuring.
    DeriveBlow();
    // Update the meter display.
    Debug.Log("RMS (vol ): " + rmsValue.ToString("F2") + " Gain " + dbValue.ToString("F1") + " dB" + "  Pitch: " + pitchValue.ToString("F0") + " Hz");

    if (Time.time - lastUpdate > SpectrumRefreshTime)
    {
        source.GetSpectrumData(spectrum, 0, FFTWindow.Blackman);
        for (int i = 0; i < spectrum.Length; i++)
        {
            cubes[i].transform.localScale = new Vector3(1, spectrum[i] * scaleFactor, 1);
        }
        lastUpdate = Time.time;
    }
    createDisplayObjects();
}

void createDisplayObjects()
{
    for (int i = 0; i < 1024; i++)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = new Vector3(i, 0, 0);
        cubes[i] = cube;
    }
}

private void gameStart()
{
    
}

private void DeriveBlow()
{
    UpdateRecords(dbValue, dbValues);
    UpdateRecords(pitchValue, pitchValues);

    // Find the average pitch in our records (used to decipher against whistles, clicks, etc).
    float sumPitch = 0;
    foreach (float num in pitchValues)
    {
        sumPitch += num;
    }
    sumPitch /= pitchValues.Count;

    // Run our low pass filter.
    lowPassResults = LowPassFilter(dbValue);

    // Decides whether this instance of the result could be a blow or not.
    if (lowPassResults > -20  && sumPitch == 0) 
    {
        blowingTime += 1;
    } else
    {
        blowingTime = 0;
    }

    // Once enough successful blows have occured over the previous frames (requiredBlowTime), the blow is triggered.
    // This example says "blowing", or "not blowing", and also blows up a sphere.
    if (blowingTime > requiedBlowTime)
    {
        Debug.Log ( "Blowing");
        GameObject.FindGameObjectWithTag("Mongol").transform.localScale *= 1.012f;
        //GameObject.FindGameObjectWithTag("blow text").SetActive(false);
    }
    else
    {
        Debug.Log ( "Not blowing");
        GameObject.FindGameObjectWithTag("Mongol").transform.localScale *= 0.999f;
        
    }
}

// Updates a record, by removing the oldest entry and adding the newest value (val).
private void UpdateRecords(float val, List<float> record)
{
    if (record.Count > recordedLength)
    {
        record.RemoveAt(0);
    }
    record.Add(val);
}

/// Gives a result (I don't really understand this yet) based on the peak volume of the record
/// and the previous low pass results.
private float LowPassFilter(float peakVolume)
{
    return ALPHA * peakVolume + (1.0f - ALPHA) * lowPassResults;
}

private void AnalyzeSound()
{
    // Get all of our samples from the mic.
    source.GetOutputData(samples, 0);

    // Sums squared samples
    float sum = 0;
    for (int i = 0; i < SAMPLECOUNT; i++)
    {
        sum += Mathf.Pow(samples[i], 2);
    }

    // RMS is the square root of the average value of the samples.
    rmsValue = Mathf.Sqrt(sum / SAMPLECOUNT);
    dbValue = 20 * Mathf.Log10(rmsValue / REFVALUE); //calculate DB

    // Clamp it to {clamp} min
    if (dbValue < -clamp)
    {
        dbValue = -clamp;
    }

    // Gets the sound spectrum.
    source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
    float maxV = 0;
    var maxN = 0;

    // Find the highest sample.
    for (int i = 0; i < SAMPLECOUNT; i++)
    {
        if ((spectrum[i] > maxV) || !(spectrum[i] > THRESHOLD))
            continue;
        maxV = spectrum[i];
        
        //float freqN = maxN;

        maxN = i;
    }
    float freqN = maxN;// Pass the index to a float variable
    // Interpolate index using neighbours
    if (maxN > 0 && maxN < SAMPLECOUNT - 1)
            {
                float dL = spectrum[maxN - 1] / spectrum[maxN];
                float dR = spectrum[maxN + 1] / spectrum[maxN];
                freqN += 0.5f * (dR * dR - dL * dL);
               // maxN = i; // maxN is the index of max
               // Convert index to frequency                   
            }
    pitchValue = freqN * (AudioSettings.outputSampleRate/2) / SAMPLECOUNT;
}           
}
