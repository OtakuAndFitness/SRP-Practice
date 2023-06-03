using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public struct CameraBufferSettings
{
    public bool allowHDR;
    public bool copyColor, CopyColorReflection, copyDepth, copyDepthReflection;
}