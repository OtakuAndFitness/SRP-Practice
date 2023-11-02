using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class LightingPass
{
    private static readonly ProfilingSampler _sampler = new("Lighting");
    private Lighting _lighting;
    private CullingResults _cullingResults;
    private ShadowSettings _shadowSettings;
    private bool _useLightPerObject;
    private int _renderingLayerMask;

    void Render(RenderGraphContext context) => _lighting.Setup(context, _cullingResults, _shadowSettings,
        _useLightPerObject, _renderingLayerMask);

    public static void Record(RenderGraph renderGraph, Lighting lighting, CullingResults cullingResults,
        ShadowSettings shadowSettings, bool useLightPerObject, int renderingLayerMask)
    {
        using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out LightingPass pass, _sampler);
        pass._lighting = lighting;
        pass._cullingResults = cullingResults;
        pass._shadowSettings = shadowSettings;
        pass._useLightPerObject = useLightPerObject;
        pass._renderingLayerMask = renderingLayerMask;
        builder.SetRenderFunc<LightingPass>((pass, context) => pass.Render(context));
    }
}
