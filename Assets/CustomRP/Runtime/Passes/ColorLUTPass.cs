using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

using static PostFXSettings;
using static PostFXStack;

public class ColorLUTPass
{
    private static readonly ProfilingSampler _sampler = new("Color LUT");

    private static readonly int
        colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT"),
        colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters"),
        colorGradingLUTInLogCId = Shader.PropertyToID("_ColorGradingLUTInLogC"),
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
        smhRangeId = Shader.PropertyToID("_SMHRange");

    private static readonly GraphicsFormat ColorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);

    private PostFXStack _stack;
    private int colorLUTResolution;

    private TextureHandle colorLUT;

    void ConfigureColorAdjustments(CommandBuffer buffer, PostFXSettings postFXSettings)
    {
        ColorAdjustmentsSettings colorAdjustments = postFXSettings.ColorAdjustments;
        buffer.SetGlobalVector(colorAdjustmentsId, new Vector4(Mathf.Pow(2f, colorAdjustments.postExposure), colorAdjustments.contrast * 0.01f + 1f, colorAdjustments.hueShift * (1f / 360f), colorAdjustments.saturation * 0.01f + 1f));
        buffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);
    }
    
    void ConfigureWhiteBalance(CommandBuffer buffer, PostFXSettings postFXSettings)
    {
        WhiteBalanceSettings whiteBalance = postFXSettings.WhiteBalance;
        buffer.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(whiteBalance.temperature, whiteBalance.tint));
    }

    void ConfigureSplitToning(CommandBuffer buffer, PostFXSettings postFXSettings)
    {
        SplitToningSettings splitToning = postFXSettings.SplitToning;
        Color splitColor = splitToning.shadows;
        splitColor.a = splitToning.balance * 0.01f;
        buffer.SetGlobalColor(splitToningShadowsId, splitColor);
        buffer.SetGlobalColor(splitToningHighlightsId, splitToning.highlights);
    }

    void ConfigureChannelMixer(CommandBuffer buffer, PostFXSettings postFXSettings)
    {
        ChannelMixerSettings channelMixer = postFXSettings.ChannelMixer;
        buffer.SetGlobalVector(channelMixerRedId, channelMixer.red);
        buffer.SetGlobalVector(channelMixerGreenId, channelMixer.green);
        buffer.SetGlobalVector(channelMixerBlueId, channelMixer.blue);

    }

    void ConfigureShadowsMidtonesHighlights(CommandBuffer buffer, PostFXSettings postFXSettings)
    {
        ShadowsMidtonesHighlightsSettings smh = postFXSettings.ShadowsMidtonesHighlights;
        buffer.SetGlobalColor(smhShadowsId, smh.shadows.linear);
        buffer.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
        buffer.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
        buffer.SetGlobalVector(smhRangeId, new Vector4(smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highlightsEnd));
    }

    void Render(RenderGraphContext context)
    {
        PostFXSettings settings = _stack.Settings;
        CommandBuffer buffer = context.cmd;
        ConfigureColorAdjustments(buffer, settings);
        ConfigureWhiteBalance(buffer, settings);
        ConfigureSplitToning(buffer, settings);
        ConfigureChannelMixer(buffer, settings);
        ConfigureShadowsMidtonesHighlights(buffer, settings);

        int lutHeight = colorLUTResolution;
        int lutWidth = lutHeight * lutHeight;
        
        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f)));

        ToneMappingSettings.Mode mode = settings.ToneMapping.mode;
        Pass pass = Pass.ColorGradingNone + (int)mode;
        buffer.SetGlobalFloat(colorGradingLUTInLogCId, _stack.CameraBufferSettings.allowHDR && pass != Pass.ColorGradingNone ? 1f : 0f);
        _stack.Draw(buffer, colorLUT, pass);
        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(1f / lutWidth, 1f / lutWidth, lutHeight - 1f));
        buffer.SetGlobalTexture(colorGradingLUTId, colorLUT);
    }

    public static TextureHandle Record(RenderGraph renderGraph, PostFXStack stack, int colorLUTResolution)
    {
        using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out ColorLUTPass pass, _sampler);
        pass._stack = stack;
        pass.colorLUTResolution = colorLUTResolution;

        int lutHeight = colorLUTResolution;
        int lutWidth = lutHeight * lutHeight;
        var desc = new TextureDesc(lutWidth, lutHeight)
        {
            colorFormat = ColorFormat,
            name = "Color LUT"
        };
        pass.colorLUT = builder.WriteTexture(renderGraph.CreateTexture(desc));
        builder.SetRenderFunc<ColorLUTPass>(static (pass, context) => pass.Render(context));
        return pass.colorLUT;
    }
}
