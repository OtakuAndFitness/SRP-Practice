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
    // private CameraRenderer _renderer;

    private bool _requiresDepthCopy;
    private CameraRendererCopier _copier;
    private TextureHandle _depthAttachment;
    void Render(RenderGraphContext context)
    {
        CommandBuffer buffer = context.cmd;
        ScriptableRenderContext renderContext = context.renderContext;
        if (_requiresDepthCopy)
        {
            // _renderer.Draw(CameraRenderer.depthAttachmentId, BuiltinRenderTextureType.CameraTarget, true);
            // _renderer.ExecuteBuffer();
            _copier.CopyByDrawing(buffer, _depthAttachment, BuiltinRenderTextureType.CameraTarget, true);
            renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        } 
        renderContext.DrawGizmos(_copier.Camera, GizmoSubset.PreImageEffects);
        renderContext.DrawGizmos(_copier.Camera, GizmoSubset.PostImageEffects);
    }
#endif

    [Conditional("UNITY_EDITOR")]
    public static void Record(RenderGraph renderGraph, bool useIntermediateBuffer, CameraRendererCopier copier, in CameraRendererTextures textures)
    {
    #if UNITY_EDITOR
        if (Handles.ShouldRenderGizmos())
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out GizmosPass pass, _sampler);
            // pass._renderer = renderer;
            pass._requiresDepthCopy = useIntermediateBuffer;
            pass._copier = copier;
            if (useIntermediateBuffer)
            {
                pass._depthAttachment = builder.ReadTexture(textures.depthAttachment);
            }
            builder.SetRenderFunc<GizmosPass>(static (pass, context) => pass.Render(context));
        }
    #endif
    }
}
