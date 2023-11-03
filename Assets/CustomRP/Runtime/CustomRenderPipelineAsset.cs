using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//右键->Create菜单中t添加一个新的子菜单
[CreateAssetMenu(menuName="Rendering/CreateCustomRenderPipeline")]
public partial class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField] 
    CameraBufferSettings cameraBuffer = new CameraBufferSettings()
    {
        allowHDR = true,
        renderScale = 1f, 
        fxaa = new CameraBufferSettings.FXAA()
        {
            fixedThreshold = 0.0833f,
            relativeThreshold = 0.166f,
            subpixelBlending = 0.75f
        }
    };
    
    [SerializeField] 
    bool useSRPBatcher = true, useLightsPerObject = true;

    [SerializeField] 
    ShadowSettings shadowSettings = default;
    
    [SerializeField]
    PostFXSettings postFXSettings = default;

    [SerializeField]
    Shader cameraRendererShader = default;

    public enum ColorLUTResolution
    {
        _16 = 16,
        _32 = 32,
        _64 = 64
    }

    [SerializeField]
    ColorLUTResolution colorLutResolution = ColorLUTResolution._32;
    
    [Header("Deprecated Settings")]
    [SerializeField, Tooltip("Dynamic batching is no longer used.")]
    bool useDynamicBatching;

    [SerializeField, Tooltip("GPU instancing is always enabled.")]
    bool useGPUInstancing;
    
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(cameraBuffer, useSRPBatcher, useLightsPerObject, shadowSettings, postFXSettings, (int)colorLutResolution, cameraRendererShader);
    }
}
