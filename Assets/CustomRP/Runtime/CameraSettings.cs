using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class CameraSettings
{
    [RenderingLayerMaskField]
    public int renderingLayerMask = -1;
    
    public bool maskLights = false;

    public bool overridePostFX = false;

    public PostFXSettings postFXSettings = default;

    public bool copyColor = true, copyDepth = true;
    
    [Serializable]
    public struct FinalBlendMode
    {
        public BlendMode source, destination;
    }

    public FinalBlendMode finalBlendMode = new FinalBlendMode
    {
        source = BlendMode.One,
        destination = BlendMode.Zero
    };
    
}
