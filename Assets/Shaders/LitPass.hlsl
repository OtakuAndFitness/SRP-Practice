#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "ShaderLibrary/Common.hlsl"
#include "ShaderLibrary/Surface.hlsl"
#include "ShaderLibrary/Shadows.hlsl"
#include "ShaderLibrary/Light.hlsl"
#include "ShaderLibrary/BRDF.hlsl"
#include "ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    // float4 _BaseColor;
CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float, _CutOff)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float2 uv : TEXCOORD0;
    float3 normalWS : VAR_NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseMap_ST);
    output.uv = input.uv * baseST.xy + baseST.zw;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    return output;
}

float4 LitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 baseCol = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
    float4 finalCol = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    finalCol *= baseCol;

    #if _CLIPPING
        float alpha = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CutOff);
        clip(finalCol.a - alpha);
    #endif

    Surface sf;
    sf.position = input.positionWS;
    sf.normal = normalize(input.normalWS);
    sf.viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
    sf.color = finalCol.rgb;
    sf.alpha = finalCol.a;
    sf.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
    sf.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
    #if defined(_PREMULTIPY_ALPHA)
        BRDF brdf = GetBRDF(sf,true);
    #else
        BRDF brdf = GetBRDF(sf);
    #endif
    float3 color = GetLighting(sf,brdf);
    return float4(color,sf.alpha);
}

#endif