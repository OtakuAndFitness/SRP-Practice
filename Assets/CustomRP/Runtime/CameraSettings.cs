using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class CameraSettings
{
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

    [RenderingLayerMaskField]
    public int renderingLayerMask = -1;
    
    public bool overridePostFX = false;

    public PostFXSettings postFXSettings = default;

    public bool maskLights = false;
}
