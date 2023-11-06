using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class FinalPass
{
   private static readonly ProfilingSampler _sampler = new("Final");
   // private CameraRenderer _renderer;
   // private CameraSettings.FinalBlendMode _finalBlendMode;

   private CameraRendererCopier _copier;
   private TextureHandle _colorAttachment;
   
   void Render(RenderGraphContext context)
   {
      // _renderer.DrawFinal(_finalBlendMode);
      // _renderer.ExecuteBuffer();
      CommandBuffer buffer = context.cmd;
      _copier.CopyToCameraTarget(buffer, _colorAttachment);
      context.renderContext.ExecuteCommandBuffer(buffer);
      buffer.Clear();
   }

   public static void Record(RenderGraph renderGraph, CameraRendererCopier copier, in CameraRenderTextures textures)
   {
      using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out FinalPass pass, _sampler);
      // pass._renderer = renderer;
      // pass._finalBlendMode = finalBlendMode;
      pass._copier = copier;
      pass._colorAttachment = builder.ReadTexture(textures.colorAttachment);
      builder.SetRenderFunc<FinalPass>((pass, context) => pass.Render(context));
   }
}
