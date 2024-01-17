using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

using static PostFXStack;

public class BloomPass
{
    const int maxBloomPyramidLevels = 16;
    
    static readonly int
        bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling"),
        bloomThresholdId = Shader.PropertyToID("_BloomThreshold"),
        bloomIntensityId = Shader.PropertyToID("_BloomIntensity");

    private static readonly ProfilingSampler _sampler = new("Bloom");

    private readonly TextureHandle[] pyramid = new TextureHandle[2 * maxBloomPyramidLevels + 1];

    private TextureHandle colorSource, bloomResult;

    private PostFXStack _stack;

    private int stepCount;

    void Render(RenderGraphContext context)
    {
        CommandBuffer buffer = context.cmd;
        PostFXSettings.BloomSettings bloomSettings = _stack.Settings.Bloom;

        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloomSettings.threshold);
        threshold.y = threshold.x * bloomSettings.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThresholdId, threshold);
        
        _stack.Draw(buffer, colorSource, pyramid[0], bloomSettings.fadeFireflies ? Pass.BloomPrefilterFireflies : Pass.BloomPrefilter);

        int fromId = 0, toId = 2;
        int i;
        for (i = 0; i < stepCount; i++)
        {
            int midId = toId - 1;
            _stack.Draw(buffer, pyramid[fromId], pyramid[midId], Pass.BloomHorizontal);
            _stack.Draw(buffer, pyramid[midId], pyramid[toId], Pass.BloomVertical);
            fromId = toId;
            toId += 2;
        }
        
        buffer.SetGlobalFloat(bloomBucibicUpsamplingId, bloomSettings.bicubicUpsampling ? 1f : 0f);

        Pass combinePass, finalPass;
        float finalIntensity;
        if (bloomSettings.mode == PostFXSettings.BloomSettings.Mode.Additive)
        {
            combinePass = finalPass = Pass.BloomAdd;
            buffer.SetGlobalFloat(bloomIntensityId, 1f);
            finalIntensity = bloomSettings.intensity;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomScatterFinal;
            buffer.SetGlobalFloat(bloomIntensityId, bloomSettings.scatter);
            finalIntensity = Mathf.Min(bloomSettings.intensity, 1f);
        }

        if (i > 1)
        {
            toId -= 5;
            for (i -= 1; i>0; i--)
            {
                buffer.SetGlobalTexture(fxSource2Id, pyramid[toId + 1]);
                _stack.Draw(buffer, pyramid[fromId], pyramid[toId], combinePass);
                fromId = toId;
                toId -= 2;
            }
        }
        
        buffer.SetGlobalFloat(bloomIntensityId, finalIntensity);
        buffer.SetGlobalTexture(fxSource2Id, colorSource);
        _stack.Draw(buffer, pyramid[fromId], bloomResult, finalPass);
    }

    public static TextureHandle Record(RenderGraph renderGraph, PostFXStack stack, in CameraRendererTextures textures)
    {
        PostFXSettings.BloomSettings bloomSettings = stack.Settings.Bloom;
        Vector2Int size = (bloomSettings.ignoreRenderScale ? new Vector2Int(stack.Camera.pixelWidth, stack.Camera.pixelHeight) : stack.BufferSize) / 2;

        if (bloomSettings.maxIterations == 0 || bloomSettings.intensity <= 0f ||
            size.y < bloomSettings.downscaleLimit * 2 || size.x < bloomSettings.downscaleLimit * 2)
        {
            return textures.colorAttachment;
        }

        using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out BloomPass pass, _sampler);
        pass._stack = stack;
        pass.colorSource = builder.ReadTexture(textures.colorAttachment);

        var desc = new TextureDesc(size.x, size.y)
        {
            colorFormat =
                SystemInfo.GetGraphicsFormat(
                    stack.CameraBufferSettings.allowHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
            name = "Bloom Prefilter"
        };
        TextureHandle[] pyramid = pass.pyramid;
        pyramid[0] = builder.CreateTransientTexture(desc);
        size /= 2;

        int pyramidIndex = 1;
        int i;
        for (i = 0; i < bloomSettings.maxIterations; i++, pyramidIndex += 2)
        {
            if (size.y < bloomSettings.downscaleLimit || size.x < bloomSettings.downscaleLimit)
            {
                break;
            }

            desc.width = size.x;
            desc.height = size.y;
            desc.name = "Bloom Pyramid H";
            pyramid[pyramidIndex] = builder.CreateTransientTexture(desc);
            desc.name = "Bloom Pyramid V";
            pyramid[pyramidIndex + 1] = builder.CreateTransientTexture(desc);
            size /= 2;
        }

        pass.stepCount = i;

        desc.width = stack.BufferSize.x;
        desc.height = stack.BufferSize.y;
        desc.name = "Bloom Result";
        pass.bloomResult = builder.WriteTexture(renderGraph.CreateTexture(desc));
        builder.SetRenderFunc<BloomPass>(static (pass, context) => pass.Render(context));
        return pass.bloomResult;
    }
}
