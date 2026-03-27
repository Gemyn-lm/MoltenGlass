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
    [Range(2, 10)]
    public int rayBounceCount = 2;
    [Range(0, 1)] 
    public float densityMult = 0.2f;
    [Range(1, 2)]
    public float ior = 1.5f;

    [Range(0, 3)]
    public float gamma = 1.5f;

    [Range(0, 64)] public int samples = 16;

    
    public DebugMode debugMode = DebugMode.None;

    void Start()
    {
        InitRenderTexture();
        _cam = Camera.main;
        _light = FindFirstObjectByType<Light>();
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
        int kernel = computeShader.FindKernel("CSMain");
        
        // View and light info
        computeShader.SetMatrix("_CameraToWorld", _cam.cameraToWorldMatrix);
        computeShader.SetMatrix("_CameraInverseProjection", _cam.projectionMatrix.inverse);
        computeShader.SetVector("_CameraPosition", _cam.transform.position);
        computeShader.SetVector("_LightDirection", _light.transform.forward.normalized);
        
        // Params
        computeShader.SetFloat("_BlendStrength", blendStrength);
        computeShader.SetFloat("_SphereSize", pointSize);
        computeShader.SetFloat("_ior", ior);
        computeShader.SetFloat("_reflectivity", reflectionRatio);
        
        // Simu params
        computeShader.SetInt("_Samples", samples); 
        computeShader.SetInt("_MaxBounces", rayBounceCount);
        computeShader.SetFloat("_Gamma", gamma);
        computeShader.SetInt("_FrameIndex", Time.frameCount);
        
        // Debug
        computeShader.SetInt("_DebugModeIndex", (int)debugMode);
        computeShader.SetFloat("_DensityMutl", densityMult);
        
        // Textures
        computeShader.SetTexture(kernel, "_SkyboxCubemap", skyboxCubemap);
        computeShader.SetTexture(kernel, "Result", _renderTexture);
        
        
        if (transform.childCount > 0)
        {
            Point.PointData[] shapeDataList = new Point.PointData[transform.childCount];
            

            int i = 0;
            foreach (Transform child in transform)
            {
                shapeDataList[i] = child.GetComponent<Point>().GetPointData();
                i++;
            }
            ComputeBuffer buffer = new ComputeBuffer(transform.childCount, Point.PointData.DataSize);
            buffer.SetData(shapeDataList);
            computeShader.SetBuffer(kernel, "shapeBuffer", buffer);
            computeShader.SetInt("shapeCount", shapeDataList.Length);
        }
        computeShader.Dispatch(kernel, _renderTexture.width / 8, _renderTexture.height / 8, 1);
    }

    void OnDisable()
    {
        if (_renderTexture != null)
            _renderTexture.Release();
    }
}