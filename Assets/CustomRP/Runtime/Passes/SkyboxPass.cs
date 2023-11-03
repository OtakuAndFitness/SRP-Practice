using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class SkyboxPass
{
	static readonly ProfilingSampler _sampler = new("Skybox");

	Camera _camera;

	void Render(RenderGraphContext context)
	{
		context.renderContext.ExecuteCommandBuffer(context.cmd);
		context.cmd.Clear();
		context.renderContext.DrawSkybox(_camera);
	}

	public static void Record(RenderGraph renderGraph, Camera camera)
	{
		if (camera.clearFlags == CameraClearFlags.Skybox)
		{
			using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out SkyboxPass pass, _sampler);
			pass._camera = camera;
			builder.SetRenderFunc<SkyboxPass>((pass, context) => pass.Render(context));
		}
	}
}
