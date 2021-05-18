using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerObjectMaterialProperties : MonoBehaviour
{
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    
    [SerializeField]
    Color baseColor = Color.white;

    static MaterialPropertyBlock mpb;


    private void OnValidate()
    {
        if (mpb == null)
        {
            mpb = new MaterialPropertyBlock();
            
        }
        
        mpb.SetColor(baseColorId,baseColor);
        
        GetComponent<Renderer>().SetPropertyBlock(mpb);
    }

    void Awake()
    {
        OnValidate();
    }
}
