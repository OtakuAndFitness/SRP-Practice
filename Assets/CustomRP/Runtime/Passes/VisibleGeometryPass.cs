using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class VisibleGeometryPass
{
   private static readonly ProfilingSampler _sampler = new("Visible Geometry");
   private CameraRenderer _renderer;
   
   bool _useDynamicBatching, _useGPUInstancing, _useLightsPerObject;

   private int _renderingLayerMask;

   void Render(RenderGraphContext context) => _renderer.DrawVisibleGeometry(_useDynamicBatching, _useGPUInstancing,
      _useLightsPerObject, _renderingLayerMask);

   public static void Record(RenderGraph renderGraph, CameraRenderer renderer, bool useDynamicBatching,
      bool useGPUInstancing, bool useLightsPerObject, int renderingLayerMask)
   {
      using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out VisibleGeometryPass pass, _sampler);
      pass._renderer = renderer;
      pass._useDynamicBatching = useDynamicBatching;
      pass._useGPUInstancing = useGPUInstancing;
      pass._useLightsPerObject = useLightsPerObject;
      pass._renderingLayerMask = renderingLayerMask;
      builder.SetRenderFunc<VisibleGeometryPass>((pass, context) => pass.Render(context));
   }
}
