using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class CopyAttachmentsPass
{
	static readonly ProfilingSampler _sampler = new("Copy Attachments");

	// CameraRenderer _renderer;
	private bool _copyColor, _copyDepth;
	private CameraRendererCopier _copier;
	private TextureHandle _colorAttachment, _depthAttachment, _colorCopy, _depthCopy;

	private static readonly int
		colorCopyID = Shader.PropertyToID("_CameraColorTexture"),
		depthCopyID = Shader.PropertyToID("_CameraDepthTexture");

	void Render(RenderGraphContext context)
	{
		CommandBuffer buffer = context.cmd;
		if (_copyColor)
		{
			_copier.Copy(buffer, _colorAttachment, _colorCopy, false);
			buffer.SetGlobalTexture(colorCopyID, _colorCopy);
		}

		if (_copyDepth)
		{
			_copier.Copy(buffer, _depthAttachment, _depthCopy, true);
			buffer.SetGlobalTexture(depthCopyID, _depthCopy);
		}

		if (CameraRendererCopier.RequiresRenderTargetResetAfterCopy)
		{
			buffer.SetRenderTarget(_colorAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, _depthAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
		}
		context.renderContext.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	public static void Record(RenderGraph renderGraph, bool copyColor, bool copyDepth, CameraRendererCopier copier, in CameraRenderTextures textures)
	{
		if (copyColor || copyDepth)
		{
			using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out CopyAttachmentsPass pass, _sampler);
			// pass._renderer = renderer;
			pass._copyColor = copyColor;
			pass._copyDepth = copyDepth;
			pass._copier = copier;

			pass._colorAttachment = builder.ReadTexture(textures.colorAttachment);
			pass._depthAttachment = builder.ReadTexture(textures.depthAttachment);
			if (copyColor)
			{
				pass._colorCopy = builder.WriteTexture(textures.colorCopy);
			}

			if (copyDepth)
			{
				pass._depthCopy = builder.WriteTexture(textures.depthCopy);
			}
			builder.SetRenderFunc<CopyAttachmentsPass>(static (pass, context) => pass.Render(context));
		}
		
	}
}
