using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class PostFXStack
{ 
    const string cmbName = "Post FX";

    CommandBuffer cmb = new CommandBuffer
    {
        name = cmbName
    };
    
    enum Pass
    {
        BloomHorizontal,
        BloomVertical,
        BloomAdd,
        BloomScatter,
        BloomScatterFinal,
        BloomPrefilter,
        BloomPrefilterFireflies,
        ToneMappingReinhard,
        ToneMappingNeutral,
        ToneMappingACES,
        Copy
    }

    const int maxBloomPyramidLevels = 16;

    int
        fxSoundId = Shader.PropertyToID("_PostFXSource"),
        fxSource2Id = Shader.PropertyToID("_PostFXSource2"),
        bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling"),
        bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter"),
        bloomThresholdId = Shader.PropertyToID("_BloomThreshold"),
        bloomIntensityId = Shader.PropertyToID("_BloomIntensity"),
        bloomResultId = Shader.PropertyToID("_BloomResult");

    int bloomPyramidId;
    
    ScriptableRenderContext context;

    Camera camera;

    PostFXSettings postFXSettings;

    bool useHDR;
    
    public bool isActive => postFXSettings != null;

    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 0; i < maxBloomPyramidLevels * 2; i++)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    bool DoBloom(int sourceId)
    {
        // cmb.BeginSample("Bloom");
        PostFXSettings.BloomSettings bloom = postFXSettings.Bloom;
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
        if (bloom.maxIterations == 0 || bloom.intensity <= 0f || height < bloom.downscaleLimits * 2 || width < bloom.downscaleLimits * 2)
        {
            // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            // cmb.EndSample("Bloom");
            return false;
        }
        cmb.BeginSample("Bloom");
        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        cmb.SetGlobalVector(bloomThresholdId, threshold);
        
        RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        cmb.GetTemporaryRT(bloomPrefilterId, width,height,0,FilterMode.Bilinear,format);
        Draw(sourceId,bloomPrefilterId,bloom.fadeFireflies ? Pass.BloomPrefilterFireflies : Pass.BloomPrefilter);
        width /= 2;
        height /= 2;
        
        int fromId = bloomPrefilterId, toId = bloomPyramidId + 1;
        int i;
        for (i = 0; i < bloom.maxIterations; i++)
        {
            if (height < bloom.downscaleLimits || width < bloom.downscaleLimits)
            {
                break;
            }
            int midId = toId - 1;
            cmb.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            cmb.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);
            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }

        cmb.ReleaseTemporaryRT(bloomPrefilterId);
        cmb.SetGlobalFloat(bloomBucibicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f);
        Pass combinePass, finalPass;
        float finalIntensity;
        if (bloom.mode == PostFXSettings.BloomSettings.Mode.Additive)
        {
            combinePass = finalPass = Pass.BloomAdd;
            cmb.SetGlobalFloat(bloomIntensityId, 1f);
            finalIntensity = bloom.intensity;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomScatterFinal;
            cmb.SetGlobalFloat(bloomIntensityId, bloom.scatter);
            finalIntensity = Mathf.Min(bloom.intensity, 0.95f);
        }
        if (i > 1)
        {
            // Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            cmb.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;
        
            for (i -= 1; i > 0; i--)
            {
                cmb.SetGlobalTexture(fxSource2Id, toId +1);
                Draw(fromId, toId, combinePass);
                cmb.ReleaseTemporaryRT(fromId);
                cmb.ReleaseTemporaryRT(toId + 1);
                fromId = toId;
                toId -= 2;
            }
        }
        else
        {
            cmb.ReleaseTemporaryRT(bloomPyramidId);
        }
        cmb.SetGlobalFloat(bloomIntensityId, finalIntensity);
        cmb.SetGlobalTexture(fxSource2Id, sourceId);
        cmb.GetTemporaryRT(bloomResultId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, format);
        Draw(fromId, bloomResultId, finalPass);
        cmb.ReleaseTemporaryRT(fromId);
        cmb.EndSample("Bloom");
        return true;
    }

    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings postFXSettings, bool useHDR)
    {
        this.context = context;
        this.camera = camera;
        this.postFXSettings = camera.cameraType <= CameraType.SceneView ? postFXSettings : null;
        this.useHDR = useHDR;
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        // cmb.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        if (DoBloom(sourceId))
        {
            DoToneMapping(bloomResultId);
            cmb.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            DoToneMapping(sourceId);
        }
        context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        cmb.SetGlobalTexture(fxSoundId, from);
        cmb.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        cmb.DrawProcedural(Matrix4x4.identity, postFXSettings.Material, (int)pass, MeshTopology.Triangles,3);
    }

    void DoToneMapping(int sourceId)
    {
        PostFXSettings.ToneMappingSettings.Mode mode = postFXSettings.ToneMapping.mode;
        Pass pass = mode < 0 ? Pass.Copy : Pass.ToneMappingACES + (int)mode;
        Draw(sourceId, BuiltinRenderTextureType.CameraTarget, pass);
    }
}
