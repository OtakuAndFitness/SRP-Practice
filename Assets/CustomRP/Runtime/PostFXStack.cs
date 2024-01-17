using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PostFXStack
{
    public enum Pass
    {
        BloomHorizontal,
        BloomVertical,
        BloomAdd,
        BloomScatter,
        BloomScatterFinal,
        BloomPrefilter,
        BloomPrefilterFireflies,
        ColorGradingNone,
        ColorGradingACES,
        ColorGradingNeutral,
        ColorGradingReinhard,
        Copy,
        ApplyColorGrading,
        ApplyColorGradingWithLuma,
        FinalRescale,
        FXAA,
        FXAAWithLuma
    }
    
    public static readonly int 
        fxSourceId = Shader.PropertyToID("_PostFXSource"),
        fxSource2Id = Shader.PropertyToID("_PostFXSource2"),
        finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend"),
        finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");

    private static readonly Rect fullViewRect = new Rect(0, 0, 1, 1);

    public CameraBufferSettings CameraBufferSettings
    {
        get;
        set;
    }

    public Vector2Int BufferSize
    {
        get;
        set;
    }

    public Camera Camera
    {
        get;
        set;
    }

    public CameraSettings.FinalBlendMode FinalBlendMode
    {
        get;
        set;
    }

    public PostFXSettings Settings
    {
        get;
        set;
    }

    public void Draw(CommandBuffer buffer, RenderTargetIdentifier to, Pass pass)
    {
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, Settings.Material, (int)pass, MeshTopology.Triangles,3);
    }

    public void Draw(CommandBuffer buffer, RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, Settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    public void DrawFinal(CommandBuffer buffer, RenderTargetIdentifier from, Pass pass)
    {
        buffer.SetGlobalFloat(finalSrcBlendId, (float)FinalBlendMode.source);
        buffer.SetGlobalFloat(finalDstBlendId, (float)FinalBlendMode.destination);
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, FinalBlendMode.destination == BlendMode.Zero && Camera.rect == fullViewRect ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        buffer.SetViewport(Camera.pixelRect);
        buffer.DrawProcedural(Matrix4x4.identity, Settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }
}
