using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class PostFXPass
{
    private static readonly ProfilingSampler _sampler = new("Post FX");
    private PostFXStack _postFXStack;

    void Render(RenderGraphContext context) => _postFXStack.Render(context, CameraRenderer.colorAttachmentId);

    public static void Record(RenderGraph renderGraph, PostFXStack postFXStack)
    {
        using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out PostFXPass pass, _sampler);
        pass._postFXStack = postFXStack;
        builder.SetRenderFunc<PostFXPass>((pass, context) => pass.Render(context));
    }
}
