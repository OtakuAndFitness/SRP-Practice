using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    ScriptableRenderContext context;
    
    Camera camera;
    
    const string cmbName = "Custom Command Buffer";
    
    CommandBuffer cmb = new CommandBuffer()
    {
        name = cmbName
    };
    
    //摄像机剔除结果
    CullingResults crs;
    static ShaderTagId unlitId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId litId = new ShaderTagId("CustomLight");
    
    static int framebufferId = Shader.PropertyToID("_CameraFrameBuffer");

    Lighting lighting = new Lighting();

    PostFXStack postFXStack = new PostFXStack();
    
    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing, bool useLightPerObject, ShadowSettings shadowSettings, PostFXSettings postFXSettings)
    {
        this.context = context;
        this.camera = camera;
        
        PrepareBuffer();
        
        PrepareForSceneWindow();

        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }
        cmb.BeginSample(SampleName);
        ExecuteBuffer();
        //设置光照信息，包含阴影信息，但阴影自己有个脚本来处理
        lighting.Setup(context,crs,shadowSettings, useLightPerObject);
        postFXStack.Setup(context, camera,postFXSettings);
        cmb.EndSample(SampleName);
        
        Setup();
        
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing, useLightPerObject);
        
        //暴露srp不支持的shader
        DrawUnsupportShaders();
        
        //绘制Gizmos
        DrawGizmosBeforeFX();
        if (postFXStack.isActive)
        {
            postFXStack.Render(framebufferId);
        }
        DrawGizmosAfterFX();
        Cleanup();
        
        //context发送的渲染命令都是缓冲的，所以要通过submit来提交命令
        Submit();
    }

    bool Cull(float maxDistance)
    {
        ScriptableCullingParameters parameters;
        if (camera.TryGetCullingParameters(out parameters))
        {
            parameters.shadowDistance = Mathf.Min(maxDistance, camera.farClipPlane);
            crs = context.Cull(ref parameters);
            return true;
        }

        return false;
    }

    //设置相机的属性和矩阵
    void Setup()
    {
        context.SetupCameraProperties(camera);
        //得到相机清除状态
        CameraClearFlags ccfs = camera.clearFlags;

        if (postFXStack.isActive)
        {
            if (ccfs > CameraClearFlags.Color)
            {
                ccfs = CameraClearFlags.Color;
            }
            cmb.GetTemporaryRT(framebufferId, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Bilinear, RenderTextureFormat.Default);
            cmb.SetRenderTarget(framebufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        }
        //设置相机清除状态
        cmb.ClearRenderTarget(ccfs<=CameraClearFlags.Depth,ccfs == CameraClearFlags.Color,ccfs == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        cmb.BeginSample(SampleName);
        ExecuteBuffer();//为了采样
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject)
    {
        PerObjectData lightsPerObjectFlags = useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
        
        SortingSettings sss = new SortingSettings()
        {
            criteria = SortingCriteria.CommonOpaque
        };
        //设置渲染的pass和排序模式
        DrawingSettings dss = new DrawingSettings(unlitId,sss)
        {
            //设置渲染时批处理的使用状态
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ReflectionProbes | lightsPerObjectFlags
        };
        //渲染CustomLit表示的pass块
        dss.SetShaderPassName(1,litId);
        //哪些类型的渲染队列会被渲染
        FilteringSettings fss = new FilteringSettings(RenderQueueRange.opaque);
        //先渲染不透明物体
        context.DrawRenderers(crs,ref dss,ref fss);
        
        //再渲染天空盒
        context.DrawSkybox(camera);
        
        //最后渲染透明物体
        sss.criteria = SortingCriteria.CommonTransparent;
        dss.sortingSettings = sss;
        fss.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(crs,ref dss,ref fss);


    }

    void Submit()
    {
        cmb.EndSample(SampleName);
        ExecuteBuffer();//真正执行
        context.Submit();
    }

    void Cleanup()
    {
        lighting.Cleanup();
        if (postFXStack.isActive)
        {
            cmb.ReleaseTemporaryRT(framebufferId);
        }
    }
}
