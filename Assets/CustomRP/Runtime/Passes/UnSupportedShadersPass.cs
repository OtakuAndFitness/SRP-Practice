using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class UnSupportedShadersPass
{
#if UNITY_EDITOR
    private static readonly ProfilingSampler _sampler = new("Unsupported Shaders");
    private CameraRenderer _renderer;

    void Render(RenderGraphContext context) => _renderer.DrawUnsupportedShaders();
#endif

    [Conditional("UNITY_EDITOR")]
    public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
    {
#if UNITY_EDITOR
        using RenderGraphBuilder builder =
            renderGraph.AddRenderPass(_sampler.name, out UnSupportedShadersPass pass, _sampler);
        pass._renderer = renderer;
        builder.SetRenderFunc<UnSupportedShadersPass>((pass, context) => pass.Render(context));
#endif
    }
}
