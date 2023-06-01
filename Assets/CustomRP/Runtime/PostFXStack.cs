using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static PostFXSettings;

public partial class PostFXStack
{ 
    const string cmbName = "Post FX";

    CommandBuffer cmb = new CommandBuffer
    {
        name = cmbName
    };
    
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
        Final
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
        colorGradingLUTInLogId = Shader.PropertyToID("_ColorGradingLUTInLog");

    int
        finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend"),
        finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");
            
    int bloomPyramidId, colorLUTResolution;
    
    ScriptableRenderContext context;

    Camera camera;

    PostFXSettings postFXSettings;

    bool useHDR;

    CameraSettings.FinalBlendMode finalBlendMode;

    public bool isActive => postFXSettings != null;

    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 1; i < maxBloomPyramidLevels * 2; i++)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    bool DoBloom(int sourceId)
    {
        // cmb.BeginSample("Bloom");
        PostFXSettings.BloomSettings bloom = postFXSettings.Bloom;
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
        if (bloom.maxIterations == 0 || bloom.intensity <= 0f || height < bloom.downscaleLimits * 2 || width < bloom.downscaleLimits * 2)
        {
            // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            // cmb.EndSample("Bloom");
            return false;
        }
        cmb.BeginSample("Bloom");
        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        cmb.SetGlobalVector(bloomThresholdId, threshold);
        
        RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        cmb.GetTemporaryRT(bloomPrefilterId, width,height,0,FilterMode.Bilinear,format);
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
            cmb.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            cmb.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);
            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }

        cmb.ReleaseTemporaryRT(bloomPrefilterId);
        cmb.SetGlobalFloat(bloomBucibicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f);
        
        Pass combinePass, finalPass;
        float finalIntensity;
        if (bloom.mode == PostFXSettings.BloomSettings.Mode.Additive)
        {
            combinePass = finalPass = Pass.BloomAdd;
            cmb.SetGlobalFloat(bloomIntensityId, 1f);
            finalIntensity = bloom.intensity;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomScatterFinal;
            cmb.SetGlobalFloat(bloomIntensityId, bloom.scatter);
            finalIntensity = Mathf.Min(bloom.intensity, 1f);
        }
        if (i > 1)
        {
            // Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            cmb.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;
        
            for (i -= 1; i > 0; i--)
            {
                cmb.SetGlobalTexture(fxSource2Id, toId +1);
                Draw(fromId, toId, combinePass);
                cmb.ReleaseTemporaryRT(fromId);
                cmb.ReleaseTemporaryRT(toId + 1);
                fromId = toId;
                toId -= 2;
            }
        }
        else
        {
            cmb.ReleaseTemporaryRT(bloomPyramidId);
        }
        cmb.SetGlobalFloat(bloomIntensityId, finalIntensity);
        cmb.SetGlobalTexture(fxSource2Id, sourceId);
        cmb.GetTemporaryRT(bloomResultId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, format);
        Draw(fromId, bloomResultId, finalPass);
        cmb.ReleaseTemporaryRT(fromId);
        cmb.EndSample("Bloom");
        return true;
    }

    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings postFXSettings, bool useHDR, int colorLUTResolution, CameraSettings.FinalBlendMode finalBlendMode)
    {
        this.context = context;
        this.camera = camera;
        this.postFXSettings = camera.cameraType <= CameraType.SceneView ? postFXSettings : null;
        this.useHDR = useHDR;
        this.colorLUTResolution = colorLUTResolution;
        this.finalBlendMode = finalBlendMode;
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        // cmb.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        if (DoBloom(sourceId))
        {
            DoColorGradingAndToneMapping(bloomResultId);
            cmb.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            DoColorGradingAndToneMapping(sourceId);
        }
        context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        cmb.SetGlobalTexture(fxSoundId, from);
        cmb.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        cmb.DrawProcedural(Matrix4x4.identity, postFXSettings.Material, (int)pass, MeshTopology.Triangles,3);
    }
    
    void DrawFinal(RenderTargetIdentifier from)
    {
        cmb.SetGlobalFloat(finalSrcBlendId, (float)finalBlendMode.source);
        cmb.SetGlobalFloat(finalDstBlendId, (float)finalBlendMode.destination);
        cmb.SetGlobalTexture(fxSoundId, from);
        cmb.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, finalBlendMode.destination == BlendMode.Zero ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        cmb.SetViewport(camera.pixelRect);
        cmb.DrawProcedural(Matrix4x4.identity, postFXSettings.Material, (int)Pass.Final, MeshTopology.Triangles,3);
    }

    void ConfigureColorAdjustments()
    {
        ColorAdjustmentsSettings colorAdjustments = postFXSettings.ColorAjustments;
        cmb.SetGlobalVector(colorAdjustmentsId, new Vector4(Mathf.Pow(2f, colorAdjustments.postExposure), colorAdjustments.contrast * 0.01f + 1f, colorAdjustments.hueShift * (1f / 360f), colorAdjustments.saturation * 0.01f + 1f));
        cmb.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);
    }

    void ConfigureWhiteBalance()
    {
        WhiteBalanceSettings whiteBalance = postFXSettings.WhiteBalance;
        cmb.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(whiteBalance.temperature, whiteBalance.tint));
    }

    void ConfigureSplitToning()
    {
        SplitToningSettings splitToning = postFXSettings.SplitToning;
        Color splitColor = splitToning.shadows;
        splitColor.a = splitToning.balance * 0.01f;
        cmb.SetGlobalColor(splitToningShadowsId, splitColor);
        cmb.SetGlobalColor(splitToningHighlightsId, splitToning.highlights);
    }

    void ConfigureChannelMixer()
    {
        ChannelMixerSettings channelMixer = postFXSettings.ChannelMixer;
        cmb.SetGlobalVector(channelMixerRedId, channelMixer.red);
        cmb.SetGlobalVector(channelMixerGreenId, channelMixer.green);
        cmb.SetGlobalVector(channelMixerBlueId, channelMixer.blue);

    }

    void ConfigureShaodwsMidtonesHighlights()
    {
        ShadowsMidtonesHighlightsSettings smh = postFXSettings.ShadowsMidtonesHighlights;
        cmb.SetGlobalColor(smhShadowsId, smh.shadows.linear);
        cmb.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
        cmb.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
        cmb.SetGlobalVector(smhRangeId, new Vector4(smh.shadowsStart, smh.shadowsEnd, smh.hightlightsStart, smh.highlightsEnd));
    }

    void DoColorGradingAndToneMapping(int sourceId)
    {
        ConfigureColorAdjustments();
        ConfigureWhiteBalance();
        ConfigureSplitToning();
        ConfigureChannelMixer();
        ConfigureShaodwsMidtonesHighlights();

        int lutHeight = colorLUTResolution;
        int lutWidth = lutHeight * lutHeight;
        cmb.GetTemporaryRT(colorGradingLUTId, lutWidth, lutHeight, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
        cmb.SetGlobalVector(colorGradingLUTParametersId, new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f)));
        
        ToneMappingSettings.Mode mode = postFXSettings.ToneMapping.mode;
        Pass pass = Pass.ColorGradingNone + (int)mode;
        cmb.SetGlobalFloat(colorGradingLUTInLogId, useHDR && pass != Pass.ColorGradingNone ? 1f : 0f);
        Draw(sourceId, colorGradingLUTId, pass);
        
        cmb.SetGlobalVector(colorGradingLUTParametersId, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f));
        DrawFinal(sourceId);
        cmb.ReleaseTemporaryRT(colorGradingLUTId);
    }
}
