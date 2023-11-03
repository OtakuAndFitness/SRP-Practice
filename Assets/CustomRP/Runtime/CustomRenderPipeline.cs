using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{
    CameraRenderer renderer;
    bool useLightsPerObject;
    ShadowSettings shadowSettings;
    PostFXSettings postFXSettings;
    int colorLUTResolution;
    CameraBufferSettings cameraBufferSettings;
    private readonly RenderGraph _renderGraph = new RenderGraph("Custom SRP Render Graph");
    
    public CustomRenderPipeline(CameraBufferSettings cameraBufferSettings, bool useSRPBatcher, bool useLightsPerObject, ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution, Shader cameraRendererShader)
    {
        this.cameraBufferSettings = cameraBufferSettings;
        // this.useDynamicBatching = useDynamicBatching;
        // this.useGPUInstancing = useGPUInstancing;
        this.useLightsPerObject = useLightsPerObject;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        this.shadowSettings = shadowSettings;
        this.postFXSettings = postFXSettings;
        this.colorLUTResolution = colorLUTResolution;
        renderer = new CameraRenderer(cameraRendererShader);
        //为了烘焙光衰减的正确，但在unity2021.3中有没有这个方法烘焙光看起来都一样
        InitializeForEditor();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        
    }

    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            renderer.Render(_renderGraph, context, cameras[i], cameraBufferSettings, useLightsPerObject, shadowSettings, postFXSettings, colorLUTResolution);
        }
        _renderGraph.EndFrame();
    }
    
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DisposeForEditor();
        renderer.Dispose();
        _renderGraph.Cleanup();
    }
}
