using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public readonly struct CameraRendererCopier
{
    private static readonly int 
        sourceTextureID = Shader.PropertyToID("_SourceTexture"),
        srcBlendID = Shader.PropertyToID("_CameraSrcBlend"),
        dstBlendID = Shader.PropertyToID("_CameraDstBlend");

    private static readonly Rect fullViewRect = new(0f, 0f, 1f, 1f); 
    
    private static readonly bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
    public static bool RequiresRenderTargetResetAfterCopy => !copyTextureSupported;
    
    public readonly Camera Camera => _camera;
    
    private readonly Material _material;
    
    private readonly Camera _camera;

    private readonly CameraSettings.FinalBlendMode _finalBlendMode;

    public CameraRendererCopier(Material material, Camera camera, CameraSettings.FinalBlendMode finalBlendMode)
    {
        _material = material;
        _camera = camera;
        _finalBlendMode = finalBlendMode;
    }

    public readonly void CopyToCameraTarget(CommandBuffer buffer, RenderTargetIdentifier from)
    {
        buffer.SetGlobalFloat(srcBlendID, (float)_finalBlendMode.source);
        buffer.SetGlobalFloat(dstBlendID, (float)_finalBlendMode.destination);
        buffer.SetGlobalTexture(sourceTextureID, from);
        buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, _finalBlendMode.destination == BlendMode.Zero && _camera.rect == fullViewRect ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        buffer.SetViewport(_camera.pixelRect);
        buffer.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
        buffer.SetGlobalFloat(srcBlendID, 1f);
        buffer.SetGlobalFloat(dstBlendID, 0f);
    }

    public readonly void Copy(CommandBuffer buffer, RenderTargetIdentifier from, RenderTargetIdentifier to,
        bool isDepth)
    {
        if (copyTextureSupported)
        {
            buffer.CopyTexture(from, to);
        }
        else
        {
            CopyByDrawing(buffer, from, to, isDepth);
        }
    }

    public readonly void CopyByDrawing(CommandBuffer buffer, RenderTargetIdentifier from, RenderTargetIdentifier to, bool isDepth)
    {
        buffer.SetGlobalTexture(sourceTextureID, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.SetViewport(_camera.pixelRect);
        buffer.DrawProcedural(Matrix4x4.identity, _material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
    }
}
