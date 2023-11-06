using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class PostFXPass
{
    private static readonly ProfilingSampler _sampler = new("Post FX");
    private PostFXStack _postFXStack;
    private TextureHandle _colorAttachment;

    void Render(RenderGraphContext context) => _postFXStack.Render(context, _colorAttachment);

    public static void Record(RenderGraph renderGraph, PostFXStack postFXStack, in CameraRenderTextures textures)
    {
        using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out PostFXPass pass, _sampler);
        pass._postFXStack = postFXStack;
        pass._colorAttachment = builder.ReadTexture(textures.colorAttachment);
        builder.SetRenderFunc<PostFXPass>((pass, context) => pass.Render(context));
    }
}
