using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class SetupPass
{
    private static readonly ProfilingSampler _sampler = new("Setup");
    
    private CameraRenderer _renderer;
    void Render(RenderGraphContext context) => _renderer.Setup();

    public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
    {
        using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out SetupPass pass, _sampler);
        pass._renderer = renderer;
        builder.SetRenderFunc<SetupPass>((pass, context) => pass.Render(context));
    }

}