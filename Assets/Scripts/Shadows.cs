using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{ 
    const string cmbName = "Shadows";

    CommandBuffer cmb = new CommandBuffer()
    {
        name = cmbName
    };

    ScriptableRenderContext context;
    ShadowSettings shadowSettings;

    const int maxShadowedDirectionalLightCount = 1;

    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
    }

    ShadowedDirectionalLight[] shadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    int ShadowedDirectionalLightCount;
    
    static int directionalShadowAtlasId = Shader.PropertyToID("directionalShadowAtlasId");
    
    CullingResults crs;

    public void SetUp(ScriptableRenderContext context, CullingResults crs, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.crs = crs;
        this.shadowSettings = shadowSettings;
        ShadowedDirectionalLightCount = 0;
        // cmb.BeginSample(cmbName);
        // SetUpDirectionalLight();
        // SetupLights();
        // cmb.EndSample(cmbName);
        // context.ExecuteCommandBuffer(cmb);
        // cmb.Clear();
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }

    public void ReverseDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None &&
            light.shadowStrength > 0f && crs.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds))
        {
            shadowedDirectionalLights[ShadowedDirectionalLightCount++] = new ShadowedDirectionalLight
            {
                visibleLightIndex = visibleLightIndex
            };
        }
    }

    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int)shadowSettings.directional.atlasSize;
        cmb.GetTemporaryRT(directionalShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        cmb.SetRenderTarget(directionalShadowAtlasId, RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        cmb.ClearRenderTarget(true,false,Color.clear);
        cmb.BeginSample(cmbName);
        ExecuteBuffer();
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, atlasSize);
        }
        cmb.EndSample(cmbName);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows(int index, int tileSize)
    {
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(crs, light.visibleLightIndex);
        crs.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, 0, 1,Vector3.zero, tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData shadowSplitData);
        shadowDrawingSettings.splitData = shadowSplitData;
        cmb.SetViewProjectionMatrices(viewMatrix,projectionMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowDrawingSettings);
    }

    public void CleanUp()
    {
        cmb.ReleaseTemporaryRT(directionalShadowAtlasId);
        ExecuteBuffer();
    }
}

