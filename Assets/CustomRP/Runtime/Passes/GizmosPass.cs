using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class GizmosPass
{
#if UNITY_EDITOR
    private static readonly ProfilingSampler _sampler = new("Gizmos");
    private CameraRenderer _renderer;

    void Render(RenderGraphContext context)
    {
        if (_renderer.useIntermediateBuffer)
        {
            _renderer.Draw(CameraRenderer.depthAttachmentId, BuiltinRenderTextureType.CameraTarget, true);
            _renderer.ExecuteBuffer();
        }
        context.renderContext.DrawGizmos(_renderer.camera, GizmoSubset.PreImageEffects);
        context.renderContext.DrawGizmos(_renderer.camera, GizmoSubset.PostImageEffects);
    }
#endif

    [Conditional("UNITY_EDITOR")]
    public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
    {
    #if UNITY_EDITOR
        if (Handles.ShouldRenderGizmos())
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out GizmosPass pass, _sampler);
            pass._renderer = renderer;
            builder.SetRenderFunc<GizmosPass>((pass, context) => pass.Render(context));
        }
    #endif
    }
}
