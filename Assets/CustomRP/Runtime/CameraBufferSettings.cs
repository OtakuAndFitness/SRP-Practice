using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CameraBufferSettings
{
    public bool allowHDR;
    public bool copyColor, CopyColorReflection, copyDepth, copyDepthReflection;

    [Range(CameraRenderer.renderScaleMin, CameraRenderer.renderScaleMax)] 
    public float renderScale;

    public enum BicubicRescalingMode{Off, UpOnly, UpAndDown}
    public BicubicRescalingMode bicubicRescaling;

}