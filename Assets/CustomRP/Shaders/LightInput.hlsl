#ifndef CUSTOM_LIGHT_INPUT_INCLUDED
#define CUSTOM_LIGHT_INPUT_INCLUDED

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_EmissionMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _CutOff)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
    UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


float2 TransformBaseUV(float2 baseUV)
{
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

float4 GetBase(float2 baseUV)
{
    float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseColor);
    return map * color;
}

float GetCutOff()
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_CutOff);
}

float GetMetallic()
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Metallic);
}

float GetSmoothness()
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Smoothness);
}

float3 GetEmission(float2 baseUV)
{
    float4 map = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, baseUV);
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_EmissionColor);
    return map.rgb * color.rgb;
}

float GetFresnel()
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Fresnel);
}


#endif