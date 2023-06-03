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
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true, useLightsPerObject = true;

    [SerializeField] 
    ShadowSettings shadowSettings = default;
    
    [SerializeField]
    PostFXSettings postFXSettings = default;

    [SerializeField]
    Shader cameraRendererShader = default;
    
    [Serializable]
    public struct CameraBufferSettings
    {
        public bool allowHDR;
        public bool copyColor, CopyColorReflection, copyDepth, copyDepthReflection;
    }

    [SerializeField] 
    CameraBufferSettings cameraBuffer = new CameraBufferSettings()
    {
        allowHDR = true
    };
    
    public enum ColorLUTResolution
    {
        _16 = 16,
        _32 = 32,
        _64 = 64
    }

    [SerializeField]
    ColorLUTResolution colorLutResolution = ColorLUTResolution._32;
    
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(cameraBuffer,useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadowSettings, postFXSettings, (int)colorLutResolution, cameraRendererShader);
    }
}
