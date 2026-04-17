using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;

public enum DebugMode
{
    None, Hit, Normal, RayDirection, Density, BounceCount
}

[ExecuteInEditMode]
public class RayMaster : MonoBehaviour
{
    public ComputeShader computeShader;
    private RenderTexture _renderTexture;
    public RawImage rawImage;
    private Camera _cam;
    public Light _light;
    public Cubemap skyboxCubemap;

    [Range(0, 5)]
    public float blendStrength = 1.5f;
    [Range(0, 5)]
    public float pointSize = 1f;

    [Range(0, 1)] 
    public float reflectionRatio = 0.5f;
    [Range(2, 30)]
    public int rayBounceCount = 2;
    [Range(0, 1)] 
    public float densityMult = 0.2f;
    [Range(1, 2)]
    public float ior = 1.5f;

    [Range(0, 3)]
    public float gamma = 1.5f;

    [Range(0, 64)] public int samples = 16;

    [Range(0f, 0.01f)] public float epsilon;

    public DebugMode debugMode = DebugMode.None;

    private ComputeBuffer _shapeBuffer;
    private int kernel;
    static class ShaderIDs
    {
        public static readonly int CameraToWorld          = Shader.PropertyToID("_CameraToWorld");
        public static readonly int CameraInverseProjection= Shader.PropertyToID("_CameraInverseProjection");
        public static readonly int CameraPosition         = Shader.PropertyToID("_CameraPosition");
        public static readonly int LightDirection         = Shader.PropertyToID("_LightDirection");
        public static readonly int BlendStrength          = Shader.PropertyToID("_BlendStrength");
        public static readonly int SphereSize             = Shader.PropertyToID("_SphereSize");
        public static readonly int Ior                    = Shader.PropertyToID("_ior");
        public static readonly int Reflectivity           = Shader.PropertyToID("_reflectivity");
        public static readonly int Samples                = Shader.PropertyToID("_Samples");
        public static readonly int MaxBounces             = Shader.PropertyToID("_MaxBounces");
        public static readonly int Gamma                  = Shader.PropertyToID("_Gamma");
        public static readonly int FrameIndex             = Shader.PropertyToID("_FrameIndex");
        public static readonly int Epsilon                = Shader.PropertyToID("_Epsilon");
        public static readonly int DebugModeIndex         = Shader.PropertyToID("_DebugModeIndex");
        public static readonly int DensityMult            = Shader.PropertyToID("_DensityMutl");
        public static readonly int SkyboxCubemap          = Shader.PropertyToID("_SkyboxCubemap");
        public static readonly int Result                 = Shader.PropertyToID("Result");
        public static readonly int ShapeBuffer            = Shader.PropertyToID("shapeBuffer");
        public static readonly int ShapeCount             = Shader.PropertyToID("shapeCount");
    }
    
    public Point[] points;
    
    void Start()
    {
        InitRenderTexture();
        _cam = Camera.main;
        _light = FindFirstObjectByType<Light>();
        kernel =  computeShader.FindKernel("CSMain");
        
        points = new Point[transform.childCount];
        int i = 0;
        foreach (Transform child in transform)
        {
            points[i] = child.GetComponent<Point>();
            i++;
        }
    }

    void InitRenderTexture()
    {
        if (_renderTexture != null)
            _renderTexture.Release();

        _renderTexture = new RenderTexture(Screen.width, Screen.height, 0)
        {
            enableRandomWrite = true
        };
        _renderTexture.Create();
        rawImage.texture = _renderTexture;
    }

    void Update()
    {

        computeShader.SetMatrix(ShaderIDs.CameraToWorld,           _cam.cameraToWorldMatrix);
        computeShader.SetMatrix(ShaderIDs.CameraInverseProjection, _cam.projectionMatrix.inverse);
        computeShader.SetVector(ShaderIDs.CameraPosition,          _cam.transform.position);
        computeShader.SetVector(ShaderIDs.LightDirection,          _light.transform.forward.normalized);

        computeShader.SetFloat(ShaderIDs.BlendStrength,  blendStrength);
        computeShader.SetFloat(ShaderIDs.SphereSize,     pointSize);
        computeShader.SetFloat(ShaderIDs.Ior,            ior);
        computeShader.SetFloat(ShaderIDs.Reflectivity,   reflectionRatio);

        computeShader.SetInt  (ShaderIDs.Samples,        samples);
        computeShader.SetInt  (ShaderIDs.MaxBounces,      rayBounceCount);
        computeShader.SetFloat(ShaderIDs.Gamma,           gamma);
        computeShader.SetInt  (ShaderIDs.FrameIndex,      Time.frameCount);
        computeShader.SetFloat(ShaderIDs.Epsilon,         epsilon);

        computeShader.SetInt  (ShaderIDs.DebugModeIndex,  (int)debugMode);
        computeShader.SetFloat(ShaderIDs.DensityMult,     densityMult);

        computeShader.SetTexture(kernel, ShaderIDs.SkyboxCubemap, skyboxCubemap);
        computeShader.SetTexture(kernel, ShaderIDs.Result,        _renderTexture);
        
        
        if (transform.childCount > 0)
        {
            Point.PointData[] shapeDataList = new Point.PointData[transform.childCount];
            

            int i = 0;
            foreach (Point point in points)
            {
                shapeDataList[i] = point.GetPointData();
                i++;
            }
            if (_shapeBuffer == null || _shapeBuffer.count != transform.childCount)
            {
                _shapeBuffer?.Release();
                _shapeBuffer = new ComputeBuffer(transform.childCount, Point.PointData.DataSize);
            }
            _shapeBuffer.SetData(shapeDataList);
            computeShader.SetBuffer(kernel, ShaderIDs.ShapeBuffer, _shapeBuffer);
            computeShader.SetInt(ShaderIDs.ShapeCount, shapeDataList.Length);
        }
        computeShader.Dispatch(kernel, _renderTexture.width / 8, _renderTexture.height / 8, 1);
        
        if (Input.GetMouseButtonDown(0)) // clic gauche
        {
            ReadPixelAtMouse();
        }
    }
    
    void ReadPixelAtMouse()
    {
        Vector2 mousePos = Input.mousePosition;

        int x = (int)(mousePos.x * _renderTexture.width  / Screen.width);
        int y = (int)(mousePos.y * _renderTexture.height / Screen.height);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = _renderTexture;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
        tex.ReadPixels(new Rect(x, y, 1, 1), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;

        Color pixel = tex.GetPixel(0, 0);
        Debug.Log($"Pixel [{x}, {y}] → R:{pixel.r:F3} G:{pixel.g:F3} B:{pixel.b:F3} A:{pixel.a:F3}");

        Destroy(tex); // évite les fuites mémoire
    }

    void OnDisable()
    {
        if (_renderTexture != null)
            _renderTexture.Release();
        _shapeBuffer?.Release();
    }
}