using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderingLayerMaskFieldAttribute : PropertyAttribute
{
    [RenderingLayerMaskField] 
    public int renderingLayerMask = -1;
}
