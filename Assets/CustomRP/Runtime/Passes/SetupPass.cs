using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

//设置color & depth textures，类似urp的color & depth图
public class SetupPass
{
    private static readonly ProfilingSampler _sampler = new("Setup");
    
    // private CameraRenderer _renderer;

    private bool _useIntermediateAttachments;
    
    private TextureHandle _colorAttachment, _depthAttachment;
    
    private Vector2Int _attachmentSize;
    
    private Camera _camera;
    
    private CameraClearFlags _clearFlags;

    private static readonly int attachmentSizeID = Shader.PropertyToID("_CameraBufferSize");

    void Render(RenderGraphContext context)
    {
        context.renderContext.SetupCameraProperties(_camera);
        CommandBuffer cmd = context.cmd;
        if (_useIntermediateAttachments)
        {
            cmd.SetRenderTarget(_colorAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, _depthAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        }
        cmd.ClearRenderTarget(_clearFlags <= CameraClearFlags.Depth, _clearFlags <= CameraClearFlags.Color, _clearFlags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear);
        cmd.SetGlobalVector(attachmentSizeID, new Vector4(1f / _attachmentSize.x, 1f / _attachmentSize.y, _attachmentSize.x, _attachmentSize.y));
        context.renderContext.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }

    public static CameraRendererTextures Record(RenderGraph renderGraph, bool useIntermediateAttachments, bool copyColor, bool copyDepth, bool useHDR, Vector2Int attachmentSize, Camera camera)
    {
        using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out SetupPass pass, _sampler);
        // pass._renderer = renderer;
        pass._useIntermediateAttachments = useIntermediateAttachments;
        pass._attachmentSize = attachmentSize;
        pass._camera = camera;
        pass._clearFlags = camera.clearFlags;
        TextureHandle colorAttachment, depthAttachment;
        TextureHandle colorCopy = default, depthCopy = default;
        if (useIntermediateAttachments)
        {
            if (pass._clearFlags > CameraClearFlags.Color)
            {
                pass._clearFlags = CameraClearFlags.Color;
            }
            TextureDesc desc = new TextureDesc(attachmentSize.x, attachmentSize.y)
            {
                colorFormat = SystemInfo.GetGraphicsFormat(useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
                name = "Color Attachment"
            };
            colorAttachment = pass._colorAttachment = builder.WriteTexture(renderGraph.CreateTexture(desc));
            if (copyColor)
            {
                desc.name = "Color Copy";
                colorCopy = renderGraph.CreateTexture(desc);
            }
            desc.depthBufferBits = DepthBits.Depth32;
            desc.name = "Depth Attachment";
            depthAttachment = pass._depthAttachment = builder.WriteTexture(renderGraph.CreateTexture(desc));
            if (copyDepth)
            {
                desc.name = "Depth Copy";
                depthCopy = renderGraph.CreateTexture(desc);
            }
        }
        else
        {
            colorAttachment = depthAttachment = pass._colorAttachment = pass._depthAttachment =
                builder.WriteTexture(renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget));
        }
        //需要clear render target，该pass不能被cull
        builder.AllowPassCulling(false);
        builder.SetRenderFunc<SetupPass>(static (pass, context) => pass.Render(context));

        return new CameraRendererTextures(colorAttachment, depthAttachment, colorCopy, depthCopy);
    }

}