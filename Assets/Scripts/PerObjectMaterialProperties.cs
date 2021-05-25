using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerObjectMaterialProperties : MonoBehaviour
{
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    static int metallicId = Shader.PropertyToID("_Matallic");
    static int smoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    Color baseColor = Color.white;

    [SerializeField, Range(0, 1)]
    float metallic = 0.0f;
    [SerializeField, Range(0, 1)]
    float smoothness = 0.5f;

    static MaterialPropertyBlock mpb;


    private void OnValidate()
    {
        if (mpb == null)
        {
            mpb = new MaterialPropertyBlock();
            
        }
        
        mpb.SetColor(baseColorId,baseColor);
        mpb.SetFloat(metallicId, metallic);
        mpb.SetFloat(smoothnessId,smoothness);
        
        GetComponent<Renderer>().SetPropertyBlock(mpb);
    }

    void Awake()
    {
        OnValidate();
    }
}
