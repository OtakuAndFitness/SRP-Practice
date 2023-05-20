using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    static int 
        baseColorId = Shader.PropertyToID("_BaseColor"),
        cutoffId = Shader.PropertyToID("_CutOff"),
        metallicId = Shader.PropertyToID("_Metallic"),
        smoothnessId = Shader.PropertyToID("_Smoothness"),
        emissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField]
    Color baseColor = Color.white;

    [SerializeField, Range(0, 1)]
    float alphaCutoff = 0.5f, metallic = 0.0f, smoothness = 0.5f;

    [SerializeField, ColorUsage(false,true)]
    Color emissionColor = Color.black;

    static MaterialPropertyBlock mpb;


    void OnValidate()
    {
        if (mpb == null)
        {
            mpb = new MaterialPropertyBlock();
            
        }
        
        mpb.SetColor(baseColorId,baseColor);
        mpb.SetFloat(cutoffId, alphaCutoff);
        mpb.SetFloat(metallicId, metallic);
        mpb.SetFloat(smoothnessId,smoothness);
        mpb.SetColor(emissionColorId, emissionColor);
        
        GetComponent<Renderer>().SetPropertyBlock(mpb);
    }

    void Awake()
    {
        OnValidate();
    }
}
