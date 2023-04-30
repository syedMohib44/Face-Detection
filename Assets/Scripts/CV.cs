using System;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System.IO;

public class CV : MonoBehaviour
{
    public string requestedDeviceName = null;
    private int requestedWidth = 640;
    private int requestedHeight = 480;

    public int requestedFPS = 30;

    public bool requestedIsFrontFacing = false;

    public Toggle adjustPixelsDirectionToggle;

    public bool adjustPixelsDirection = false;

    WebCamTexture webcamTexture;

    Color32[] colors;
    Color32[] rotatedColors;

    bool hasInitDone = false;
    ScreenOrientation screenOrientation;
    int screenWidth;
    int screenHeight;
    Texture2D texture;
    private bool doprocess = true;

    public RawImage cameraView;

    //public Text inferenceText;

    public int isFrontFacing = 0;
    // public Button ExitButton;
    // public Button noProcessBtn;
    [DllImport("UnityPlugin.bundle")]
    private static extern void ProcessImage(ref IntPtr rawImage, int width, int height);

    [DllImport("UnityPlugin.bundle")]
    private static extern int returnNum();

    [DllImport("UnityPlugin.bundle")]
    private static extern int initBuffer(int width, int height);

    [DllImport("UnityPlugin.bundle")]
    private static extern IntPtr processFrame(int width, int height, IntPtr bufferAddr);


    [DllImport("UnityPlugin.bundle")]
    private static extern IntPtr rotate90Degree(int width, int height, IntPtr bufferAddr);


    [DllImport("UnityPlugin.bundle")]
    private static extern void startDetector(string conf, string weight);

    [DllImport("UnityPlugin.bundle")]
    private static extern IntPtr facedetector(int width, int height, IntPtr bufferAddr);


    string configFilePath;
    string weightFilePath;



    void FileAsync()
    {
        string fromPath = Application.streamingAssetsPath + "/";
        string toPath = Application.dataPath + "/Data/";
        string[] filesNamesToCopy = new string[] { "deploy.prototxt", "weights.caffemodel" };
        foreach (string fileName in filesNamesToCopy)
        {
            byte[] inp_ln = File.ReadAllBytes(fromPath + fileName);
            File.WriteAllBytes(toPath + fileName, inp_ln);
        }
    }


    void Start()
    {

        FileAsync();
        //init buffer
        initBuffer(requestedWidth, requestedHeight);

        webcamTexture = new WebCamTexture(WebCamTexture.devices[0].name, requestedWidth, requestedHeight, 30);
        webcamTexture.Play();

        if (colors == null || colors.Length != webcamTexture.width * webcamTexture.height)
        {
            colors = new Color32[webcamTexture.width * webcamTexture.height];
        }
        texture = new Texture2D(640, 480, TextureFormat.RGBA32, false);

        cameraView.texture = texture;

        configFilePath = Application.dataPath + "/Data/deploy.prototxt";
        weightFilePath = Application.dataPath + "/Data/weights.caffemodel";
        startDetector(configFilePath, weightFilePath);
        Debug.Log(configFilePath + " weight files initialized " + weightFilePath);
    }



    void Update()
    {
        Color32[] colors = webcamTexture.GetPixels32();
        float startTimeSeconds = Time.realtimeSinceStartup;
        if (doprocess)
        {

            GCHandle pixelHandle = GCHandle.Alloc(colors, GCHandleType.Pinned);
            IntPtr results = facedetector(webcamTexture.width, webcamTexture.height, pixelHandle.AddrOfPinnedObject());
            int bufferSize = 640 * 480 * 4;
            byte[] rawData = new byte[bufferSize];
            if (results != IntPtr.Zero)
            {
                Marshal.Copy(results, rawData, 0, bufferSize);
                texture.LoadRawTextureData(rawData);
                texture.Apply();
            }
            rawData = null;
            pixelHandle.Free();
        }
        else
        {
            GCHandle pixelHandle = GCHandle.Alloc(colors, GCHandleType.Pinned);
            IntPtr results = rotate90Degree(webcamTexture.width, webcamTexture.height, pixelHandle.AddrOfPinnedObject());
            int bufferSize = webcamTexture.width * webcamTexture.height * 4;
            byte[] rawData = new byte[bufferSize];
            if (results != IntPtr.Zero)
            {
                Marshal.Copy(results, rawData, 0, bufferSize);
                texture.LoadRawTextureData(rawData);
                texture.Apply();
            }
            rawData = null;
            pixelHandle.Free();
        }

        cameraView.texture = texture;
    }


    private void OnExitButtonClick()
    {
        Application.Quit();
    }

    private void OnNoProcessButtonClick()
    {
        if (doprocess)
            doprocess = false;
        else
            doprocess = true;

    }
}
