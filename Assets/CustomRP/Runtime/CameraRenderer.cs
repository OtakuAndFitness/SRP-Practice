using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class CameraRenderer
{
    public const float renderScaleMin = 0.1f, renderScaleMax = 2f;

    // ScriptableRenderContext context;
    
    // public Camera camera;
    
    // const string cmbName = "Custom Command Buffer";
    
    // CommandBuffer cmb = new CommandBuffer()
    // {
    //     name = cmbName
    // };
    // private CommandBuffer _buffer;
    
    //摄像机剔除结果
    // CullingResults crs;
    // static ShaderTagId unlitId = new ShaderTagId("SRPDefaultUnlit");
    // static ShaderTagId litId = new ShaderTagId("CustomLight");

    // public static int
        // colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment"),
        // depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment"),
        // colorTextureId = Shader.PropertyToID("_CameraColorTexture"),
        // depthTextureId = Shader.PropertyToID("_CameraDepthTexture"),
        // sourceTextureId = Shader.PropertyToID("_SourceTexture"),
        // srcBlendId = Shader.PropertyToID("_CameraSrcBlend"),
        // dstBlendId = Shader.PropertyToID("_CameraDstBlend"),
        // bufferSizeId = Shader.PropertyToID("_CameraBufferSize");

    // Lighting lighting = new Lighting();

    PostFXStack postFXStack = new PostFXStack();

    // public bool useHDR, useColorTexture, useDepthTexture, useIntermediateBuffer, useScaledRendering;
    // private bool useScaledRendering;
    
    static CameraSettings defaultCameraSettings = new CameraSettings();

    Material material;
    // Texture2D missingTexture;

    // static bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
    // static Rect fullViewRect = new Rect(0f, 0f, 1f, 1f);

    // Vector2Int bufferSize;


    public CameraRenderer(Shader shader) => material = CoreUtils.CreateEngineMaterial(shader);
    // {
    //     material = CoreUtils.CreateEngineMaterial(shader);
    //     missingTexture = new Texture2D(1, 1)
    //     {
    //         hideFlags = HideFlags.HideAndDontSave,
    //         name = "Missing"
    //     };
    //     missingTexture.SetPixel(0,0,Color.white * 0.5f);
    //     missingTexture.Apply();
    // }
    
    //设置相机的属性和矩阵
    // public void Setup()
    // {
    //     context.SetupCameraProperties(camera);
    //     //得到相机清除状态
    //     CameraClearFlags ccfs = camera.clearFlags;
    //
    //     // useIntermediateBuffer = useColorTexture || useDepthTexture || postFXStack.IsActive || useScaledRendering;
    //     if (useIntermediateBuffer)
    //     {
    //         if (ccfs > CameraClearFlags.Color)
    //         {
    //             ccfs = CameraClearFlags.Color;
    //         }
    //         _buffer.GetTemporaryRT(colorAttachmentId, bufferSize.x, bufferSize.y, 32, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
    //         _buffer.GetTemporaryRT(depthAttachmentId, bufferSize.x, bufferSize.y, 32, FilterMode.Point, RenderTextureFormat.Depth);
    //         _buffer.SetRenderTarget(colorAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, depthAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
    //     }
    //     //设置相机清除状态
    //     _buffer.ClearRenderTarget(ccfs<=CameraClearFlags.Depth,ccfs <= CameraClearFlags.Color,ccfs == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
    //     // cmb.BeginSample(SampleName);
    //     _buffer.SetGlobalTexture(colorTextureId, missingTexture);
    //     _buffer.SetGlobalTexture(depthTextureId, missingTexture);
    //     _buffer.SetGlobalVector(bufferSizeId, new Vector4(1f / bufferSize.x, 1f / bufferSize.y, bufferSize.x, bufferSize.y));
    //     ExecuteBuffer();//为了采样
    // }

    public void Render(RenderGraph renderGraph, ScriptableRenderContext context, Camera camera, CameraBufferSettings cameraBufferSettings, bool useLightsPerObject, ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution)
    {
        // this.context = context;
        // this.camera = camera;

        // CustomRenderPipelineCamera crpCamera = camera.GetComponent<CustomRenderPipelineCamera>();
        // CameraSettings cameraSettings = crpCamera ? crpCamera.CameraSettings : defaultCameraSettings;
        ProfilingSampler cameraSampler;
        CameraSettings cameraSettings;
        if (camera.TryGetComponent(out CustomRenderPipelineCamera crpCamera))
        {
            cameraSampler = crpCamera.Sampler;
            cameraSettings = crpCamera.Settings;
        }
        else
        {
            cameraSampler = ProfilingSampler.Get(camera.cameraType);
            cameraSettings = defaultCameraSettings;
        }

        // useDepthTexture = true;
        bool useColorTexture, useDepthTexture;
        if (camera.cameraType == CameraType.Reflection)
        {
            useColorTexture = cameraBufferSettings.copyColorReflection;
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

        bool hasActivePostFX = postFXSettings != null && postFXSettings.AreApplicableTo(camera);

        float renderScale = cameraSettings.GetRenderScale(cameraBufferSettings.renderScale);
        bool useScaledRendering = renderScale < 0.99f || renderScale > 1.01f;
        
        // PrepareBuffer();
        
        // PrepareForSceneWindow();
#if UNITY_EDITOR
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            useScaledRendering = false;
        }
#endif

        // if (!Cull(shadowSettings.maxDistance))
        // {
            // return;
        // }
        if (!camera.TryGetCullingParameters(out ScriptableCullingParameters scriptableCullingParameters))
        {
            return;
        }

        scriptableCullingParameters.shadowDistance = Mathf.Min(shadowSettings.maxDistance, camera.farClipPlane);
        CullingResults cullingResults = context.Cull(ref scriptableCullingParameters);

        // bool useHDR = cameraBufferSettings.allowHDR && camera.allowHDR;
        cameraBufferSettings.allowHDR &= camera.allowHDR;
        
        Vector2Int bufferSize = default;
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
        
        // cmb.BeginSample(SampleName);
        // cmb.SetGlobalVector(bufferSizeId, new Vector4(1f / bufferSize.x, 1f / bufferSize.y, bufferSize.x, bufferSize.y));
        // ExecuteBuffer();
        //设置光照信息，包含阴影信息，但阴影自己有个脚本来处理
        // lighting.Setup(context,crs,shadowSettings, useLightPerObject, cameraSettings.maskLights ? cameraSettings.renderingLayerMask : -1);
        cameraBufferSettings.fxaa.enabled &= cameraSettings.allowFXAA;
        // postFXStack.Setup(camera, bufferSize, postFXSettings,cameraSettings.keepAlpha, cameraBufferSettings.allowHDR, colorLUTResolution, cameraSettings.finalBlendMode, cameraBufferSettings.bicubicRescaling, cameraBufferSettings.fxaa);
        // cmb.EndSample(SampleName);
        
        // Setup();
        
        // DrawVisibleGeometry(useDynamicBatching, useGPUInstancing, useLightPerObject, cameraSettings.renderingLayerMask);
        
        //暴露srp不支持的shader
        // DrawUnsupportedShaders();
        
        //绘制Gizmos
        // DrawGizmosBeforeFX();
        // if (postFXStack.IsActive)
        // {
        //     postFXStack.Render(colorAttachmentId);
        // }
        // else if (useIntermediateBuffer)
        // {
        //     DrawFinal(cameraSettings.finalBlendMode);
        //     // Draw(colorAttachmentId, BuiltinRenderTextureType.CameraTarget);
        //     ExecuteBuffer();
        // }
        // DrawGizmosAfterFX();
        
        bool useIntermediateBuffer = useColorTexture || useDepthTexture || hasActivePostFX || useScaledRendering;

        RenderGraphParameters renderGraphParameters = new RenderGraphParameters()
        {
            commandBuffer = CommandBufferPool.Get(),
            currentFrameIndex = Time.frameCount,
            executionName = cameraSampler.name,
            rendererListCulling = true,
            scriptableRenderContext = context
        };
        // _buffer = renderGraphParameters.commandBuffer;
        using (renderGraph.RecordAndExecute(renderGraphParameters))
        {
            //Add pass here.
            // using RenderGraphBuilder builder = renderGraph.AddRenderPass("Test Pass", out CameraSettings data);
            // builder.SetRenderFunc((CameraSettings data, RenderGraphContext context) => { });
            using var _ = new RenderGraphProfilingScope(renderGraph, cameraSampler);
            
            ShadowTextures shadowTextures = LightingPass.Record(renderGraph, cullingResults, shadowSettings, useLightsPerObject, cameraSettings.maskLights ? cameraSettings.renderingLayerMask : -1);
            
            CameraRenderTextures textures = SetupPass.Record(renderGraph, useIntermediateBuffer, useColorTexture, useDepthTexture, cameraBufferSettings.allowHDR, bufferSize, camera);
            
            //opaque objects pass
            GeometryPass.Record(renderGraph, camera, cullingResults, useLightsPerObject, cameraSettings.renderingLayerMask, true, textures, shadowTextures);

            SkyboxPass.Record(renderGraph, camera, textures);

            CameraRendererCopier copier = new CameraRendererCopier(material, camera, cameraSettings.finalBlendMode);
            
            CopyAttachmentsPass.Record(renderGraph, useColorTexture, useDepthTexture, copier, textures);

            //transparent objects pass
            GeometryPass.Record(renderGraph, camera, cullingResults, useLightsPerObject, cameraSettings.renderingLayerMask, false, textures, shadowTextures);
            
            UnSupportedShadersPass.Record(renderGraph, camera, cullingResults);
            
            if (hasActivePostFX)
            {
                postFXStack.CameraBufferSettings = cameraBufferSettings;
                postFXStack.BufferSize = bufferSize;
                postFXStack.Camera = camera;
                postFXStack.FinalBlendMode = cameraSettings.finalBlendMode;
                postFXStack.Settings = postFXSettings;
                PostFXPass.Record(renderGraph, postFXStack, colorLUTResolution, cameraSettings.keepAlpha, textures);
            }else if (useIntermediateBuffer)
            {
                FinalPass.Record(renderGraph, copier, textures);
            }
            
            GizmosPass.Record(renderGraph, useIntermediateBuffer, copier, textures);
        }
        
        // lighting.Cleanup();
        
        //context发送的渲染命令都是缓冲的，所以要通过submit来提交命令
        // Submit();
        context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
        context.Submit();
        
        CommandBufferPool.Release(renderGraphParameters.commandBuffer);
    }

    // bool Cull(float maxDistance)
    // {
    //     ScriptableCullingParameters parameters;
    //     if (camera.TryGetCullingParameters(out parameters))
    //     {
    //         parameters.shadowDistance = Mathf.Min(maxDistance, camera.farClipPlane);
    //         crs = context.Cull(ref parameters);
    //         return true;
    //     }
    //
    //     return false;
    // }

    // public void ExecuteBuffer()
    // {
    //     context.ExecuteCommandBuffer(_buffer);
    //     _buffer.Clear();
    // }
    
    // public void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject, int renderingLayerMask)
    // {
    //     ExecuteBuffer();
    //     
    //     PerObjectData lightsPerObjectFlags = useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
    //     
    //     SortingSettings sss = new SortingSettings()
    //     {
    //         criteria = SortingCriteria.CommonOpaque
    //     };
    //     //设置渲染的pass和排序模式
    //     DrawingSettings dss = new DrawingSettings(unlitId,sss)
    //     {
    //         //设置渲染时批处理的使用状态
    //         enableDynamicBatching = useDynamicBatching,
    //         enableInstancing = useGPUInstancing,
    //         perObjectData = PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ReflectionProbes | lightsPerObjectFlags
    //     };
    //     //渲染CustomLit表示的pass块
    //     dss.SetShaderPassName(1,litId);
    //     //哪些类型的渲染队列会被渲染
    //     FilteringSettings fss = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: (uint)renderingLayerMask);
    //     //先渲染不透明物体
    //     context.DrawRenderers(crs,ref dss,ref fss);
    //     
    //     //再渲染天空盒
    //     context.DrawSkybox(camera);
    //     if (useColorTexture || useDepthTexture)
    //     {
    //         CopyAttachments();
    //     }
    //     
    //     //最后渲染透明物体
    //     sss.criteria = SortingCriteria.CommonTransparent;
    //     dss.sortingSettings = sss;
    //     fss.renderQueueRange = RenderQueueRange.transparent;
    //     context.DrawRenderers(crs,ref dss,ref fss);
    //
    //
    // }

    // public void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, bool isDepth = false)
    // {
    //     _buffer.SetGlobalTexture(sourceTextureId, from);
    //     _buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
    //     _buffer.DrawProcedural(Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
    // }

    // public void DrawFinal(CameraSettings.FinalBlendMode finalBlendMode)
    // {
    //     _buffer.SetGlobalFloat(srcBlendId, (float)finalBlendMode.source);
    //     _buffer.SetGlobalFloat(dstBlendId, (float)finalBlendMode.destination);
    //     _buffer.SetGlobalTexture(sourceTextureId, colorAttachmentId);
    //     _buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, finalBlendMode.destination == BlendMode.Zero && camera.rect == fullViewRect ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
    //     _buffer.SetViewport(camera.pixelRect);
    //     _buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
    //     _buffer.SetGlobalFloat(srcBlendId, 1f);
    //     _buffer.SetGlobalFloat(dstBlendId, 0f);
    // }

    // public void CopyAttachments()
    // {
    //     ExecuteBuffer();
    //     
    //     if (useColorTexture)
    //     {
    //         _buffer.GetTemporaryRT(colorTextureId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
    //     }
    //     if (copyTextureSupported)
    //     {
    //         _buffer.CopyTexture(colorAttachmentId, colorTextureId);
    //     }
    //     else
    //     {
    //         Draw(colorAttachmentId, colorTextureId);   
    //     }
    //     if (useDepthTexture)
    //     {
    //         _buffer.GetTemporaryRT(depthTextureId, bufferSize.x, bufferSize.y, 32, FilterMode.Point, RenderTextureFormat.Depth);
    //         if (copyTextureSupported)
    //         {
    //             _buffer.CopyTexture(depthAttachmentId, depthTextureId);
    //         }
    //         else
    //         {
    //             Draw(depthAttachmentId, depthTextureId, true);
    //         }
    //     }
    //     if (!copyTextureSupported)
    //     {
    //         _buffer.SetRenderTarget(colorAttachmentId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, depthAttachmentId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
    //     }
    //     
    //     ExecuteBuffer();
    // }

    // void Submit()
    // {
    //     // cmb.EndSample(SampleName);
    //     ExecuteBuffer();//真正执行
    //     context.Submit();
    // }

    // void Cleanup()
    // {
        // lighting.Cleanup();
        // if (useIntermediateBuffer)
        // {
            // _buffer.ReleaseTemporaryRT(colorAttachmentId);
            // _buffer.ReleaseTemporaryRT(depthAttachmentId);
            // if (useColorTexture)
            // {
            //     _buffer.ReleaseTemporaryRT(colorTextureId);
            // }
            // if (useDepthTexture)
            // {
            //     _buffer.ReleaseTemporaryRT(depthTextureId);
            // }
        // }

    // }
    
    public void Dispose() => CoreUtils.Destroy(material);
    // {
        // CoreUtils.Destroy(material);
        // CoreUtils.Destroy(missingTexture);
    // }
}
