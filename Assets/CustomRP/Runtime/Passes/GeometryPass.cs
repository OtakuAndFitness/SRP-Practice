using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

public class GeometryPass
{
	static readonly ProfilingSampler
		_samplerOpaque = new("Opaque Geometry"),
		_samplerTransparent = new("Transparent Geometry");

	static readonly ShaderTagId[] _shaderTagIds = {
		new("SRPDefaultUnlit"),
		new("CustomLit")
	};

	RendererListHandle _listHandle;

	void Render(RenderGraphContext context)
	{
		context.cmd.DrawRendererList(_listHandle);
		context.renderContext.ExecuteCommandBuffer(context.cmd);
		context.cmd.Clear();
	}

	public static void Record(
		RenderGraph renderGraph, Camera camera, CullingResults cullingResults,
		bool useLightsPerObject, int renderingLayerMask, bool opaque, in CameraRenderTextures textures)
	{
		ProfilingSampler sampler = opaque ? _samplerOpaque : _samplerTransparent;

		using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out GeometryPass pass, sampler);

		pass._listHandle = builder.UseRendererList(renderGraph.CreateRendererList(new RendererListDesc(_shaderTagIds, cullingResults, camera)
			{
				sortingCriteria = opaque ? SortingCriteria.CommonOpaque : SortingCriteria.CommonTransparent,
				rendererConfiguration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbeProxyVolume | (useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None),
				renderQueueRange = opaque ? RenderQueueRange.opaque : RenderQueueRange.transparent,
				renderingLayerMask = (uint)renderingLayerMask
			}));

		builder.ReadWriteTexture(textures.colorAttachment);
		builder.ReadWriteTexture(textures.depthAttachment);
		if (!opaque)
		{
			if (textures.colorCopy.IsValid())
			{
				builder.ReadTexture(textures.colorCopy);
			}

			if (textures.depthCopy.IsValid())
			{
				builder.ReadTexture(textures.depthCopy);
			}
		}
		builder.SetRenderFunc<GeometryPass>((pass, context) => pass.Render(context));
	}
}
