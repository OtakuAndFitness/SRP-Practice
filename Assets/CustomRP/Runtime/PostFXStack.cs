using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using static PostFXSettings;

public partial class PostFXStack
{ 
    // const string cmbName = "Post FX";

    // CommandBuffer cmb = new CommandBuffer
    // {
    //     name = cmbName
    // };
    private CommandBuffer _buffer;
    
    enum Pass
    {
        BloomHorizontal,
        BloomVertical,
        BloomAdd,
        BloomScatter,
        BloomScatterFinal,
        BloomPrefilter,
        BloomPrefilterFireflies,
        ColorGradingNone,
        ColorGradingACES,
        ColorGradingNeutral,
        ColorGradingReinhard,
        Copy,
        ApplyColorGrading,
        ApplyColorGradingWithLuma,
        FinalRescale,
        FXAA,
        FXAAWithLuma
    }

    const int maxBloomPyramidLevels = 16;

    int
        fxSoundId = Shader.PropertyToID("_PostFXSource"),
        fxSource2Id = Shader.PropertyToID("_PostFXSource2"),
        bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling"),
        bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter"),
        bloomThresholdId = Shader.PropertyToID("_BloomThreshold"),
        bloomIntensityId = Shader.PropertyToID("_BloomIntensity"),
        bloomResultId = Shader.PropertyToID("_BloomResult");

    int
        colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments"),
        colorFilterId = Shader.PropertyToID("_ColorFilter"),
        whiteBalanceId = Shader.PropertyToID("_WhiteBalance"),
        splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows"),
        splitToningHighlightsId = Shader.PropertyToID("_SplitToningHighlights"),
        channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed"),
        channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen"),
        channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue"),
        smhShadowsId = Shader.PropertyToID("_SMHShadows"),
        smhMidtonesId = Shader.PropertyToID("_SMHMidtones"),
        smhHighlightsId = Shader.PropertyToID("_SMHHighlights"),
        smhRangeId = Shader.PropertyToID("_SMHRange"),
        colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT"),
        colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters"),
        colorGradingLUTInLogCId = Shader.PropertyToID("_ColorGradingLUTInLogC");

    int
        copyBicubicId = Shader.PropertyToID("_CopyBicubic"),
        colorGradingResultId = Shader.PropertyToID("_ColorGradingResult"),
        finalResultId = Shader.PropertyToID("_FinalResult"),
        finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend"),
        finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");

    int fxaaConfigId = Shader.PropertyToID("_FXAAConfig");
            
    int bloomPyramidId, colorLUTResolution;
    
    // ScriptableRenderContext context;

    Camera camera;

    PostFXSettings postFXSettings;

    bool useHDR, keepAlpha;

    CameraSettings.FinalBlendMode finalBlendMode;

    public bool IsActive => postFXSettings != null;

    Vector2Int bufferSize;
    
    CameraBufferSettings.BicubicRescalingMode bicubicRescaling;

    CameraBufferSettings.FXAA fxaa;

    private const string
        fxaaQualityLowKeyword = "FXAA_QUALITY_LOW",
        fxaaQualityMediumKeyword = "FXAA_QUALITY_MEDIUM";

    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 1; i < maxBloomPyramidLevels * 2; i++)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    public void Setup(Camera camera, Vector2Int bufferSize, PostFXSettings postFXSettings, bool keepAlpha, bool useHDR, int colorLUTResolution, CameraSettings.FinalBlendMode finalBlendMode, CameraBufferSettings.BicubicRescalingMode bicubicRescaling, CameraBufferSettings.FXAA fxaa)
    {
        this.camera = camera;
        this.bufferSize = bufferSize;
        this.postFXSettings = camera.cameraType <= CameraType.SceneView ? postFXSettings : null;
        this.keepAlpha = keepAlpha;
        this.useHDR = useHDR;
        this.colorLUTResolution = colorLUTResolution;
        this.finalBlendMode = finalBlendMode;
        this.bicubicRescaling = bicubicRescaling;
        this.fxaa = fxaa;
        ApplySceneViewState();
    }

    public void Render(RenderGraphContext context, int sourceId)
    {
        _buffer = context.cmd;
        // cmb.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        if (DoBloom(sourceId))
        {
            DoFinal(bloomResultId);
            _buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            DoFinal(sourceId);
        }
        context.renderContext.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }
    
        bool DoBloom(int sourceId)
    {
        // cmb.BeginSample("Bloom");
        BloomSettings bloom = postFXSettings.Bloom;
        int width, height;
        if (bloom.ignoreRenderScale)
        {
            width = camera.pixelWidth / 2;
            height = camera.pixelHeight / 2;
        }
        else
        {
            width = bufferSize.x / 2;
            height = bufferSize.y / 2;

        }
        if (bloom.maxIterations == 0 || bloom.intensity <= 0f || height < bloom.downscaleLimits * 2 || width < bloom.downscaleLimits * 2)
        {
            // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            // cmb.EndSample("Bloom");
            return false;
        }
        _buffer.BeginSample("Bloom");
        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        _buffer.SetGlobalVector(bloomThresholdId, threshold);
        
        RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        _buffer.GetTemporaryRT(bloomPrefilterId, width,height,0,FilterMode.Bilinear,format);
        Draw(sourceId,bloomPrefilterId,bloom.fadeFireflies ? Pass.BloomPrefilterFireflies : Pass.BloomPrefilter);
        width /= 2;
        height /= 2;
        
        int fromId = bloomPrefilterId, toId = bloomPyramidId + 1;
        int i;
        for (i = 0; i < bloom.maxIterations; i++)
        {
            if (height < bloom.downscaleLimits || width < bloom.downscaleLimits)
            {
                break;
            }
            int midId = toId - 1;
            _buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            _buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);
            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }

        _buffer.ReleaseTemporaryRT(bloomPrefilterId);
        _buffer.SetGlobalFloat(bloomBucibicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f);
        
        Pass combinePass, finalPass;
        float finalIntensity;
        if (bloom.mode == BloomSettings.Mode.Additive)
        {
            combinePass = finalPass = Pass.BloomAdd;
            _buffer.SetGlobalFloat(bloomIntensityId, 1f);
            finalIntensity = bloom.intensity;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomScatterFinal;
            _buffer.SetGlobalFloat(bloomIntensityId, bloom.scatter);
            finalIntensity = Mathf.Min(bloom.intensity, 1f);
        }
        if (i > 1)
        {
            // Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            _buffer.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;
        
            for (i -= 1; i > 0; i--)
            {
                _buffer.SetGlobalTexture(fxSource2Id, toId +1);
                Draw(fromId, toId, combinePass);
                _buffer.ReleaseTemporaryRT(fromId);
                _buffer.ReleaseTemporaryRT(toId + 1);
                fromId = toId;
                toId -= 2;
            }
        }
        else
        {
            _buffer.ReleaseTemporaryRT(bloomPyramidId);
        }
        _buffer.SetGlobalFloat(bloomIntensityId, finalIntensity);
        _buffer.SetGlobalTexture(fxSource2Id, sourceId);
        _buffer.GetTemporaryRT(bloomResultId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, format);
        Draw(fromId, bloomResultId, finalPass);
        _buffer.ReleaseTemporaryRT(fromId);
        _buffer.EndSample("Bloom");
        return true;
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        _buffer.SetGlobalTexture(fxSoundId, from);
        _buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _buffer.DrawProcedural(Matrix4x4.identity, postFXSettings.Material, (int)pass, MeshTopology.Triangles,3);
    }
    
    static Rect fullViewRect = new Rect(0f, 0f, 1f, 1f);

    void DrawFinal(RenderTargetIdentifier from, Pass pass)
    {
        _buffer.SetGlobalFloat(finalSrcBlendId, (float)finalBlendMode.source);
        _buffer.SetGlobalFloat(finalDstBlendId, (float)finalBlendMode.destination);
        _buffer.SetGlobalTexture(fxSoundId, from);
        _buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, finalBlendMode.destination == BlendMode.Zero && camera.rect == fullViewRect ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        _buffer.SetViewport(camera.pixelRect);
        _buffer.DrawProcedural(Matrix4x4.identity, postFXSettings.Material, (int)pass, MeshTopology.Triangles,3);
    }

    void ConfigureColorAdjustments()
    {
        ColorAdjustmentsSettings colorAdjustments = postFXSettings.ColorAdjustments;
        _buffer.SetGlobalVector(colorAdjustmentsId, new Vector4(Mathf.Pow(2f, colorAdjustments.postExposure), colorAdjustments.contrast * 0.01f + 1f, colorAdjustments.hueShift * (1f / 360f), colorAdjustments.saturation * 0.01f + 1f));
        _buffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);
    }

    void ConfigureWhiteBalance()
    {
        WhiteBalanceSettings whiteBalance = postFXSettings.WhiteBalance;
        _buffer.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(whiteBalance.temperature, whiteBalance.tint));
    }

    void ConfigureSplitToning()
    {
        SplitToningSettings splitToning = postFXSettings.SplitToning;
        Color splitColor = splitToning.shadows;
        splitColor.a = splitToning.balance * 0.01f;
        _buffer.SetGlobalColor(splitToningShadowsId, splitColor);
        _buffer.SetGlobalColor(splitToningHighlightsId, splitToning.highlights);
    }

    void ConfigureChannelMixer()
    {
        ChannelMixerSettings channelMixer = postFXSettings.ChannelMixer;
        _buffer.SetGlobalVector(channelMixerRedId, channelMixer.red);
        _buffer.SetGlobalVector(channelMixerGreenId, channelMixer.green);
        _buffer.SetGlobalVector(channelMixerBlueId, channelMixer.blue);

    }

    void ConfigureShadowsMidtonesHighlights()
    {
        ShadowsMidtonesHighlightsSettings smh = postFXSettings.ShadowsMidtonesHighlights;
        _buffer.SetGlobalColor(smhShadowsId, smh.shadows.linear);
        _buffer.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
        _buffer.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
        _buffer.SetGlobalVector(smhRangeId, new Vector4(smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highlightsEnd));
    }

    void ConfigureFXAA()
    {
        if (fxaa.quality == CameraBufferSettings.FXAA.Quality.Low)
        {
            _buffer.EnableShaderKeyword(fxaaQualityLowKeyword);
            _buffer.DisableShaderKeyword(fxaaQualityMediumKeyword);
        }
        else if (fxaa.quality == CameraBufferSettings.FXAA.Quality.Medium)
        {
            _buffer.DisableShaderKeyword(fxaaQualityLowKeyword);
            _buffer.EnableShaderKeyword(fxaaQualityMediumKeyword);
        }
        else
        {
            _buffer.DisableShaderKeyword(fxaaQualityLowKeyword);
            _buffer.DisableShaderKeyword(fxaaQualityMediumKeyword);
        }
        _buffer.SetGlobalVector(fxaaConfigId, new Vector4(fxaa.fixedThreshold, fxaa.relativeThreshold, fxaa.subpixelBlending));

    }

    void DoFinal(int sourceId)
    {
        ConfigureColorAdjustments();
        ConfigureWhiteBalance();
        ConfigureSplitToning();
        ConfigureChannelMixer();
        ConfigureShadowsMidtonesHighlights();

        int lutHeight = colorLUTResolution;
        int lutWidth = lutHeight * lutHeight;
        _buffer.GetTemporaryRT(colorGradingLUTId, lutWidth, lutHeight, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
        _buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f)));
        
        ToneMappingSettings.Mode mode = postFXSettings.ToneMapping.mode;
        Pass pass = Pass.ColorGradingNone + (int)mode;
        _buffer.SetGlobalFloat(colorGradingLUTInLogCId, useHDR && pass != Pass.ColorGradingNone ? 1f : 0f);
        Draw(sourceId, colorGradingLUTId, pass);
        
        _buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f));
        _buffer.SetGlobalFloat(finalSrcBlendId, 1f);
        _buffer.SetGlobalFloat(finalDstBlendId,0f);
        if (fxaa.enabled)
        {
            ConfigureFXAA();
            _buffer.GetTemporaryRT(colorGradingResultId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            Draw(sourceId, colorGradingResultId, keepAlpha ? Pass.ApplyColorGrading : Pass.ApplyColorGradingWithLuma);
        }
        if (bufferSize.x == camera.pixelWidth)
        {
            if (fxaa.enabled)
            {
                DrawFinal(colorGradingResultId, keepAlpha ? Pass.FXAA : Pass.FXAAWithLuma);
                _buffer.ReleaseTemporaryRT(colorGradingResultId);
            }
            else
            {
                DrawFinal(sourceId, Pass.ApplyColorGrading);
            }
        }
        else
        {
            // cmb.SetGlobalFloat(finalSrcBlendId, 1f);
            // cmb.SetGlobalFloat(finalDstBlendId, 0f);
            _buffer.GetTemporaryRT(finalResultId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            if (fxaa.enabled)
            {
                Draw(colorGradingResultId, finalResultId, keepAlpha ? Pass.FXAA : Pass.FXAAWithLuma);
                _buffer.ReleaseTemporaryRT(colorGradingResultId);
            }
            else
            {
                Draw(sourceId, finalResultId, Pass.ApplyColorGrading);
            }
            bool bicubicSampling = bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                                   bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpOnly &&
                                   bufferSize.x < camera.pixelWidth;
            _buffer.SetGlobalFloat(copyBicubicId, bicubicSampling ? 1f : 0f);
            DrawFinal(finalResultId, Pass.FinalRescale);
            _buffer.ReleaseTemporaryRT(finalResultId);
        }
        _buffer.ReleaseTemporaryRT(colorGradingLUTId);
    }
}
