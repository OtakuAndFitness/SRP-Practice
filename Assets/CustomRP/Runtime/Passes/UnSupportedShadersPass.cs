using System.Diagnostics;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

public class UnSupportedShadersPass
{
#if UNITY_EDITOR
	static readonly ProfilingSampler _sampler = new("Unsupported Shaders");

	static readonly ShaderTagId[] _shaderTagIds = {
		new("Always"),
		new("ForwardBase"),
		new("PrepassBase"),
		new("Vertex"),
		new("VertexLMRGBM"),
		new("VertexLM")
	};

	static Material _errorMaterial;

	RendererListHandle _listHandle;

	void Render(RenderGraphContext context)
	{
		context.cmd.DrawRendererList(_listHandle);
		context.renderContext.ExecuteCommandBuffer(context.cmd);
		context.cmd.Clear();
	}
#endif

	[Conditional("UNITY_EDITOR")]
	public static void Record(
		RenderGraph renderGraph, Camera camera, CullingResults cullingResults)
	{
#if UNITY_EDITOR
		using RenderGraphBuilder builder = renderGraph.AddRenderPass(_sampler.name, out UnSupportedShadersPass pass, _sampler);

		if (_errorMaterial == null)
		{
			_errorMaterial = new(Shader.Find("Hidden/InternalErrorShader"));
		}

		pass._listHandle = builder.UseRendererList(renderGraph.CreateRendererList(
			new RendererListDesc(_shaderTagIds, cullingResults, camera)
			{
				overrideMaterial = _errorMaterial,
				renderQueueRange = RenderQueueRange.all
			}));

		builder.SetRenderFunc<UnSupportedShadersPass>(static (pass, context) => pass.Render(context));
#endif
	}
}
