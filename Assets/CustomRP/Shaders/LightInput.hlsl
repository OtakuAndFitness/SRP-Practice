#ifndef CUSTOM_LIGHT_INPUT_INCLUDED
#define CUSTOM_LIGHT_INPUT_INCLUDED

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_EmissionMap);
TEXTURE2D(_MaskMap);

TEXTURE2D(_DetailMap);
SAMPLER(sampler_DetailMap);
TEXTURE2D(_DetailNormalMap);

TEXTURE2D(_NormalMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _DetailMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
    UNITY_DEFINE_INSTANCED_PROP(float, _DetailNormalScale)
    UNITY_DEFINE_INSTANCED_PROP(float, _CutOff)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
    UNITY_DEFINE_INSTANCED_PROP(float, _Occlusion)
    UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)
    UNITY_DEFINE_INSTANCED_PROP(float, _DetailAlbedo)
    UNITY_DEFINE_INSTANCED_PROP(float, _DetailSmoothness)
    UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

struct InputConfig
{
    float2 baseUV;
    float2 detailUV;
    bool useMask;
    bool useDetail;
    Fragment fragment;
};

InputConfig GetInputConfig(float4 positionSS, float2 baseUV, float2 detailUV = 0.0)
{
    InputConfig c;
    c.fragment = GetFragment(positionSS);
    c.baseUV = baseUV;
    c.detailUV = detailUV;
    c.useMask = false;
    c.useDetail = false;
    return c;
}

float2 TransformBaseUV(float2 baseUV)
{
    float4 baseST = INPUT_PROP(_BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

float2 TransformDetailUV(float2 detailUV)
{
    float4 detailST = INPUT_PROP(_DetailMap_ST);
    return detailUV * detailST.xy + detailST.zw;
}

float4 GetDetail(InputConfig config)
{
    if (config.useDetail)
    {
        float4 detail = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, config.detailUV);
        return detail * 2.0 - 1.0;
    }
    return 0.0;
}

float4 GetMask(InputConfig config)
{
    if (config.useMask)
    {
        return SAMPLE_TEXTURE2D(_MaskMap, sampler_BaseMap, config.baseUV);
    }
    return 1.0;
}

float4 GetBase(InputConfig config)
{
    float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, config.baseUV);
    float4 color = INPUT_PROP(_BaseColor);

    if (config.useDetail)
    {
        float detail = GetDetail(config).r * INPUT_PROP(_DetailAlbedo);
        float mask = GetMask(config).b;
        map.rgb = lerp(sqrt(map.rgb), detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
        map.rgb *= map.rgb;
    }
    
    return map * color;
}

float3 GetNormalTS(InputConfig config)
{
    float4 map = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, config.baseUV);
    float scale = INPUT_PROP(_NormalScale);
    float3 normal = DecodeNormal(map,scale);

    if (config.useDetail)
    {
        map = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailMap, config.detailUV);
        scale = INPUT_PROP(_DetailNormalScale) * GetMask(config).b;
        float3 detail = DecodeNormal(map,scale);
        normal = BlendNormalRNM(normal, detail);
    }
    
    return normal;
}

float GetCutOff()
{
    return INPUT_PROP(_CutOff);
}

float GetMetallic(InputConfig config)
{
    float metallic = INPUT_PROP(_Metallic);
    metallic *= GetMask(config).r;
    return metallic;
}

float GetSmoothness(InputConfig config)
{
    float smoothness = INPUT_PROP(_Smoothness);
    smoothness *= GetMask(config).a;

    if (config.useDetail)
    {
        float detail = GetDetail(config).b * INPUT_PROP(_DetailSmoothness);
        float mask = GetMask(config).b;
        smoothness = lerp(smoothness, detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
    }
    
    return smoothness;
}

float GetOcclusion(InputConfig config)
{
    float strength = INPUT_PROP(_Occlusion);
    float occlusion = GetMask(config).g;
    occlusion = lerp(occlusion, 1.0, strength);
    return occlusion;
}

float3 GetEmission(InputConfig config)
{
    float4 map = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, config.baseUV);
    float4 color = INPUT_PROP(_EmissionColor);
    return map.rgb * color.rgb;
}

float GetFresnel()
{
    return INPUT_PROP(_Fresnel);
}

float GetFinalAlpha(float alpha)
{
    return INPUT_PROP(_ZWrite) ? 1.0 : alpha;
}


#endif