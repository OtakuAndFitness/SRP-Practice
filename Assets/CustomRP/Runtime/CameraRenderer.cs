using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    public const float renderScaleMin = 0.1f, renderScaleMax = 2f;

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

    private static int
        colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment"),
        depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment"),
        colorTextureId = Shader.PropertyToID("_CameraColorTexture"),
        depthTextureId = Shader.PropertyToID("_CameraDepthTexture"),
        sourceTextureId = Shader.PropertyToID("_SourceTexture"),
        srcBlendId = Shader.PropertyToID("_CameraSrcBlend"),
        dstBlendId = Shader.PropertyToID("_CameraDstBlend"),
        bufferSizeId = Shader.PropertyToID("_CameraBufferSize");

    Lighting lighting = new Lighting();

    PostFXStack postFXStack = new PostFXStack();

    bool useHDR, useColorTexture, useDepthTexture, useIntermediateBuffer, useScaledRendering;
    
    static CameraSettings defaultCameraSettings = new CameraSettings();

    Material material;
    Texture2D missingTexture;

    static bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;

    Vector2Int bufferSize;


    public CameraRenderer(Shader shader)
    {
        material = CoreUtils.CreateEngineMaterial(shader);
        missingTexture = new Texture2D(1, 1)
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = "Missing"
        };
        missingTexture.SetPixel(0,0,Color.white * 0.5f);
        missingTexture.Apply();
    }
    
    //设置相机的属性和矩阵
    void Setup()
    {
        context.SetupCameraProperties(camera);
        //得到相机清除状态
        CameraClearFlags ccfs = camera.clearFlags;

        useIntermediateBuffer = useColorTexture || useDepthTexture || postFXStack.isActive || useScaledRendering;
        if (useIntermediateBuffer)
        {
            if (ccfs > CameraClearFlags.Color)
            {
                ccfs = CameraClearFlags.Color;
            }
            cmb.GetTemporaryRT(colorAttachmentId, bufferSize.x, bufferSize.y, 32, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            cmb.GetTemporaryRT(depthAttachmentId, bufferSize.x, bufferSize.y, 32, FilterMode.Point, RenderTextureFormat.Depth);
            cmb.SetRenderTarget(colorAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, depthAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        }
        //设置相机清除状态
        cmb.ClearRenderTarget(ccfs<=CameraClearFlags.Depth,ccfs == CameraClearFlags.Color,ccfs == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        cmb.BeginSample(SampleName);
        cmb.SetGlobalTexture(colorTextureId, missingTexture);
        cmb.SetGlobalTexture(depthTextureId, missingTexture);
        ExecuteBuffer();//为了采样
    }

    public void Render(ScriptableRenderContext context, Camera camera, CameraBufferSettings cameraBufferSettings, bool useDynamicBatching, bool useGPUInstancing, bool useLightPerObject, ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution)
    {
        this.context = context;
        this.camera = camera;

        CustomRenderPipelineCamera crpCamera = camera.GetComponent<CustomRenderPipelineCamera>();
        CameraSettings cameraSettings = crpCamera ? crpCamera.CameraSettings : defaultCameraSettings;

        // useDepthTexture = true;
        if (camera.cameraType == CameraType.Reflection)
        {
            useColorTexture = cameraBufferSettings.CopyColorReflection;
            useDepthTexture = cameraBufferSettings.copyDepthReflection;
        }
        else
        {
            useColorTexture = cameraBufferSettings.copyColor && cameraSettings.copyColor;
            useDepthTexture = cameraBufferSettings.copyDepth && cameraSettings.copyDepth;
        }

        if (cameraSettings.overridePostFX)
        {
            postFXSettings = cameraSettings.postFXSettings;
        }

        float renderScale = cameraSettings.GetRenderScale(cameraBufferSettings.renderScale);
        useScaledRendering = renderScale < 0.99f || renderScale > 1.01f;
        
        PrepareBuffer();
        
        PrepareForSceneWindow();

        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }
        useHDR = cameraBufferSettings.allowHDR && camera.allowHDR;
        if (useScaledRendering)
        {
            renderScale = Mathf.Clamp(renderScale, renderScaleMin, renderScaleMax);
            bufferSize.x = (int)(camera.pixelWidth * renderScale);
            bufferSize.y = (int)(camera.pixelHeight * renderScale);
        }
        else
        {
            bufferSize.x = camera.pixelWidth;
            bufferSize.y = camera.pixelHeight;
        }
        
        cmb.BeginSample(SampleName);
        cmb.SetGlobalVector(bufferSizeId, new Vector4(1f / bufferSize.x, 1f / bufferSize.y, bufferSize.x, bufferSize.y));
        ExecuteBuffer();
        //设置光照信息，包含阴影信息，但阴影自己有个脚本来处理
        lighting.Setup(context,crs,shadowSettings, useLightPerObject, cameraSettings.maskLights ? cameraSettings.renderingLayerMask : -1);
        cameraBufferSettings.fxaa.enabled &= cameraSettings.allowFXAA;
        postFXStack.Setup(context, camera, bufferSize, postFXSettings,cameraSettings.keepAlpha, useHDR, colorLUTResolution, cameraSettings.finalBlendMode, cameraBufferSettings.bicubicRescaling, cameraBufferSettings.fxaa);
        cmb.EndSample(SampleName);
        
        Setup();
        
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing, useLightPerObject, cameraSettings.renderingLayerMask);
        
        //暴露srp不支持的shader
        DrawUnsupportShaders();
        
        //绘制Gizmos
        DrawGizmosBeforeFX();
        if (postFXStack.isActive)
        {
            postFXStack.Render(colorAttachmentId);
        }
        else if (useIntermediateBuffer)
        {
            DrawFinal(cameraSettings.finalBlendMode);
            // Draw(colorAttachmentId, BuiltinRenderTextureType.CameraTarget);
            ExecuteBuffer();
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

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject, int renderingLayerMask)
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
        FilteringSettings fss = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: (uint)renderingLayerMask);
        //先渲染不透明物体
        context.DrawRenderers(crs,ref dss,ref fss);
        
        //再渲染天空盒
        context.DrawSkybox(camera);
        if (useColorTexture || useDepthTexture)
        {
            CopyAttachments();
        }
        
        //最后渲染透明物体
        sss.criteria = SortingCriteria.CommonTransparent;
        dss.sortingSettings = sss;
        fss.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(crs,ref dss,ref fss);


    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, bool isDepth = false)
    {
        cmb.SetGlobalTexture(sourceTextureId, from);
        cmb.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        cmb.DrawProcedural(Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
    }

    void DrawFinal(CameraSettings.FinalBlendMode finalBlendMode)
    {
        cmb.SetGlobalFloat(srcBlendId, (float)finalBlendMode.source);
        cmb.SetGlobalFloat(dstBlendId, (float)finalBlendMode.destination);
        cmb.SetGlobalTexture(sourceTextureId, colorAttachmentId);
        cmb.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, finalBlendMode.destination == BlendMode.Zero ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        cmb.SetViewport(camera.pixelRect);
        cmb.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
        cmb.SetGlobalFloat(srcBlendId, 1f);
        cmb.SetGlobalFloat(dstBlendId, 0f);
    }

    void CopyAttachments()
    {
        if (useColorTexture)
        {
            cmb.GetTemporaryRT(colorTextureId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
        }
        if (copyTextureSupported)
        {
            cmb.CopyTexture(colorAttachmentId, colorTextureId);
        }
        else
        {
            Draw(colorAttachmentId, colorTextureId);   
        }
        if (useDepthTexture)
        {
            cmb.GetTemporaryRT(depthTextureId, bufferSize.x, bufferSize.y, 32, FilterMode.Point, RenderTextureFormat.Depth);
            if (copyTextureSupported)
            {
                cmb.CopyTexture(depthAttachmentId, depthTextureId);
            }
            else
            {
                Draw(depthAttachmentId, depthTextureId, true);
            }
        }
        if (!copyTextureSupported)
        {
            cmb.SetRenderTarget(colorAttachmentId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, depthAttachmentId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }
        
        ExecuteBuffer();
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
        if (useIntermediateBuffer)
        {
            cmb.ReleaseTemporaryRT(colorAttachmentId);
            cmb.ReleaseTemporaryRT(depthAttachmentId);
            if (useColorTexture)
            {
                cmb.ReleaseTemporaryRT(colorTextureId);
            }
            if (useDepthTexture)
            {
                cmb.ReleaseTemporaryRT(depthTextureId);
            }
        }

    }
    
    public void Dispose()
    {
        CoreUtils.Destroy(material);
        CoreUtils.Destroy(missingTexture);
    }
}
