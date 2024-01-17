using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

using static PostFXStack;

public class PostFXPass
{
    private static readonly ProfilingSampler
        groupSampler = new("Post FX"),
        finalSampler = new("Final Post FX");
    
    static readonly int
        copyBicubicId = Shader.PropertyToID("_CopyBicubic"),
        fxaaConfigId = Shader.PropertyToID("_FXAAConfig");
    
    static readonly GlobalKeyword 
        fxaaQualityLowKeyword = GlobalKeyword.Create("FXAA_QUALITY_LOW"),
        fxaaQualityMediumKeyword = GlobalKeyword.Create("FXAA_QUALITY_MEDIUM");

    private static readonly GraphicsFormat ColorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
        
    private PostFXStack _postFXStack;

    private bool keepAlpha;

    enum ScaleMode
    {
        None,
        Linear,
        Bicubic
    }

    private ScaleMode _scaleMode;
    
    private TextureHandle colorSource, colorGradingResult, scaleResult;

    void ConfigureFXAA(CommandBuffer buffer)
    {
        CameraBufferSettings.FXAA fxaa = _postFXStack.CameraBufferSettings.fxaa;
        
        buffer.SetKeyword(fxaaQualityLowKeyword, fxaa.quality == CameraBufferSettings.FXAA.Quality.Low);
        buffer.SetKeyword(fxaaQualityMediumKeyword, fxaa.quality == CameraBufferSettings.FXAA.Quality.Medium);
        
        buffer.SetGlobalVector(fxaaConfigId, new Vector4(fxaa.fixedThreshold, fxaa.relativeThreshold, fxaa.subpixelBlending));

    }
    
    void Render(RenderGraphContext context)
    {
        CommandBuffer buffer = context.cmd;
        buffer.SetGlobalFloat(finalSrcBlendId, 1f);
        buffer.SetGlobalFloat(finalDstBlendId, 0f);

        RenderTargetIdentifier finalSource;
        Pass finalPass;
        if (_postFXStack.CameraBufferSettings.fxaa.enabled)
        {
            finalSource = colorGradingResult;
            finalPass = keepAlpha ? Pass.FXAA : Pass.FXAAWithLuma;
            ConfigureFXAA(buffer);
            _postFXStack.Draw(buffer, colorSource, finalSource, keepAlpha ? Pass.ApplyColorGrading : Pass.ApplyColorGradingWithLuma);
        }
        else
        {
            finalSource = colorSource;
            finalPass = Pass.ApplyColorGrading;
        }

        if (_scaleMode == ScaleMode.None)
        {
            _postFXStack.DrawFinal(buffer, finalSource, finalPass);
        }
        else
        {
            _postFXStack.Draw(buffer, finalSource, scaleResult, finalPass);
            buffer.SetGlobalFloat(copyBicubicId, _scaleMode == ScaleMode.Bicubic ? 1f : 0f);
            _postFXStack.DrawFinal(buffer, scaleResult, Pass.FinalRescale);
        }
        context.renderContext.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public static void Record(RenderGraph renderGraph, PostFXStack postFXStack, int colorLUTResolution, bool keepAlpha, in CameraRendererTextures textures)
    {
        using var _ = new RenderGraphProfilingScope(renderGraph, groupSampler);

        TextureHandle colorSource = BloomPass.Record(renderGraph, postFXStack, textures);

        TextureHandle colorLUT = ColorLUTPass.Record(renderGraph, postFXStack, colorLUTResolution);
        
        using RenderGraphBuilder builder = renderGraph.AddRenderPass(finalSampler.name, out PostFXPass pass, finalSampler);
        pass.keepAlpha = keepAlpha;
        pass._postFXStack = postFXStack;
        pass.colorSource = builder.ReadTexture(colorSource);
        builder.ReadTexture(colorLUT);

        if (postFXStack.BufferSize.x == postFXStack.Camera.pixelWidth)
        {
            pass._scaleMode = ScaleMode.None;
        }
        else
        {
            pass._scaleMode =
                postFXStack.CameraBufferSettings.bicubicRescaling ==
                CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                postFXStack.CameraBufferSettings.bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpOnly &&
                postFXStack.BufferSize.x < postFXStack.Camera.pixelWidth
                    ? ScaleMode.Bicubic
                    : ScaleMode.Linear;
        }

        bool appyFXAA = postFXStack.CameraBufferSettings.fxaa.enabled;
        if (appyFXAA || pass._scaleMode != ScaleMode.None)
        {
            var desc = new TextureDesc(postFXStack.BufferSize.x, postFXStack.BufferSize.y)
            {
                colorFormat = ColorFormat
            };
            if (appyFXAA)
            {
                desc.name = "Color Grading Result";
                pass.colorGradingResult = builder.CreateTransientTexture(desc);
            }

            if (pass._scaleMode != ScaleMode.None)
            {
                desc.name = "Scaled Result";
                pass.scaleResult = builder.CreateTransientTexture(desc);
            }
        }
        
        builder.SetRenderFunc<PostFXPass>(static (pass, context) => pass.Render(context));
    }
}
