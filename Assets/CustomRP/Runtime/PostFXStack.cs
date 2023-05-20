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
        BloomCombine,
        BloomPrefilter,
        Copy
    }

    const int maxBloomPyramidLevels = 16;

    private int
        fxSoundId = Shader.PropertyToID("_PostFXSource"),
        fxSource2Id = Shader.PropertyToID("_PostFXSource2"),
        bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling"),
        bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter"),
        bloomThresholdId = Shader.PropertyToID("_BloomThreshold"),
        bloomIntensityId = Shader.PropertyToID("_BloomIntensity");

    int bloomPyramidId;
    
    ScriptableRenderContext context;

    Camera camera;

    PostFXSettings postFXSettings;
    
    public bool isActive => postFXSettings != null;

    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 0; i < maxBloomPyramidLevels * 2; i++)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    void DoBloom(int sourceId)
    {
        cmb.BeginSample("Bloom");
        PostFXSettings.BloomSettings bloom = postFXSettings.Bloom;
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
        if (bloom.maxIterations == 0 || bloom.intensity <= 0f || height < bloom.downscaleLimits * 2 || width < bloom.downscaleLimits * 2)
        {
            Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            cmb.EndSample("Bloom");
            return;
        }

        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        cmb.SetGlobalVector(bloomThresholdId, threshold);
        
        RenderTextureFormat format = RenderTextureFormat.Default;
        cmb.GetTemporaryRT(bloomPrefilterId, width,height,0,FilterMode.Bilinear,format);
        Draw(sourceId,bloomPrefilterId,Pass.BloomPrefilter);
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
        cmb.SetGlobalFloat(bloomIntensityId, 1f);
        if (i > 1)
        {
            // Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            cmb.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;
        
            for (i -= 1; i > 0; i--)
            {
                cmb.SetGlobalTexture(fxSource2Id, toId +1);
                Draw(fromId, toId, Pass.BloomCombine);
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
        cmb.SetGlobalFloat(bloomIntensityId, bloom.intensity);
        cmb.SetGlobalTexture(fxSource2Id, sourceId);
        Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.BloomCombine);
        cmb.ReleaseTemporaryRT(fromId);
        cmb.EndSample("Bloom");
    }

    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings postFXSettings)
    {
        this.context = context;
        this.camera = camera;
        this.postFXSettings = camera.cameraType <= CameraType.SceneView ? postFXSettings : null;
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        // cmb.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        DoBloom(sourceId);
        context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        cmb.SetGlobalTexture(fxSoundId, from);
        cmb.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        cmb.DrawProcedural(Matrix4x4.identity, postFXSettings.Material, (int)pass, MeshTopology.Triangles,3);
    }
}