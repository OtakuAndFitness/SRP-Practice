using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class CopyAttachmentsPass
{
	static readonly ProfilingSampler _sampler = new("Copy Attachments");

	CameraRenderer _renderer;

	void Render(RenderGraphContext context) => _renderer.CopyAttachments();

	public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
	{
		using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out CopyAttachmentsPass pass, _sampler);
		pass._renderer = renderer;
		builder.SetRenderFunc<CopyAttachmentsPass>((pass, context) => pass.Render(context));
	}
}
