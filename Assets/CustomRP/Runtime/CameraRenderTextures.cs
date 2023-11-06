using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

public readonly ref struct CameraRenderTextures
{
    public readonly TextureHandle colorAttachment, depthAttachment, colorCopy, depthCopy;

    public CameraRenderTextures(TextureHandle colorAttachment, TextureHandle depthAttachment, TextureHandle colorCopy, TextureHandle depthCopy)
    {
        this.colorAttachment = colorAttachment;
        this.depthAttachment = depthAttachment;
        this.colorCopy = colorCopy;
        this.depthCopy = depthCopy;

    }
}