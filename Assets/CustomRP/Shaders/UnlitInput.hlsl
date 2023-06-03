#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

TEXTURE2D(_BaseMap);
TEXTURE2D(_DistortionMap);
SAMPLER(sampler_BaseMap);


UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _CutOff)
    UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeDistance)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeRange)
    UNITY_DEFINE_INSTANCED_PROP(float, _SoftParticlesDistance)
    UNITY_DEFINE_INSTANCED_PROP(float, _SoftParticlesRange)
    UNITY_DEFINE_INSTANCED_PROP(float, _DistortionStrength)
    UNITY_DEFINE_INSTANCED_PROP(float, _DistortionBlend)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct InputConfig
{
    float4 color;
    bool flipbookBlending;
    float3 flipbookUVB;
    float2 baseUV;
    Fragment fragment;
    bool nearFade;
    bool softParticles;
};

InputConfig GetInputConfig(float4 positionSS, float2 baseUV)
{
    InputConfig c;
    c.fragment = GetFragment(positionSS);
    c.baseUV = baseUV;
    c.color = 1.0;
    c.flipbookUVB = 0.0;
    c.flipbookBlending = false;
    c.nearFade = false;
    c.softParticles = false;
    return c;
}

float2 TransformBaseUV(float2 baseUV)
{
    float4 baseST = INPUT_PROP(_BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

float4 GetBase(InputConfig config)
{
    float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, config.baseUV);
    if (config.flipbookBlending)
    {
        map = lerp(map, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, config.flipbookUVB.xy), config.flipbookUVB.z);
    }
    if (config.nearFade)
    {
        float nearAttenuation = (config.fragment.depth - INPUT_PROP(_NearFadeDistance)) / INPUT_PROP(_NearFadeRange);
        map.a *= saturate(nearAttenuation); 
    }
    if (config.softParticles)
    {
        float depthDelta = config.fragment.bufferDepth - config.fragment.depth;
        float nearAttenuation = (depthDelta - INPUT_PROP(_SoftParticlesDistance)) / INPUT_PROP(_SoftParticlesRange);
        map.a *= saturate(nearAttenuation);
    }
    float4 color = INPUT_PROP(_BaseColor);
    return map * color * config.color;
}

float2 GetDistortion(InputConfig config)
{
    float4 distortion = SAMPLE_TEXTURE2D(_DistortionMap, sampler_BaseMap, config.baseUV);
    if (config.flipbookBlending)
    {
        distortion = lerp(distortion, SAMPLE_TEXTURE2D(_DistortionMap, sampler_BaseMap, config.flipbookUVB.xy), config.flipbookUVB.z);
    }
    return DecodeNormal(distortion, INPUT_PROP(_DistortionStrength)).xy;
}

float GetDistortionBlend(InputConfig config)
{
    return INPUT_PROP(_DistortionBlend);
}

float GetCutOff()
{
    return INPUT_PROP(_CutOff);
}

float3 GetEmission(InputConfig config)
{
    return GetBase(config).rgb;
}

float GetMetallic(InputConfig config)
{
    return 0.0;
}

float GetSmoothness(InputConfig config)
{
    return 0.0;
}

float GetFinalAlpha(float alpha)
{
    return INPUT_PROP(_ZWrite) ? 1.0 : alpha;
}


#endif