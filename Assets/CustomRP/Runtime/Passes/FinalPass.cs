using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class FinalPass
{
   private static readonly ProfilingSampler _sampler = new("Final");
   private CameraRenderer _renderer;
   private CameraSettings.FinalBlendMode _finalBlendMode;

   void Render(RenderGraphContext context)
   {
      _renderer.DrawFinal(_finalBlendMode);
      _renderer.ExecuteBuffer();
   }

   public static void Record(RenderGraph renderGraph, CameraRenderer renderer, CameraSettings.FinalBlendMode finalBlendMode)
   {
      using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out FinalPass pass, _sampler);
      pass._renderer = renderer;
      pass._finalBlendMode = finalBlendMode;
      builder.SetRenderFunc<FinalPass>((pass, context) => pass.Render(context));
   }
}
