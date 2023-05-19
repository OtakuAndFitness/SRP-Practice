﻿#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
//如果使用的是PCF 3x3
#if defined(_DIRECTIONAL_PCF3)
    //需要4个滤波样本
    #define DIRECTIONAL_FILTER_SAMPLES 4
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
    //需要9个滤波样本
    #define DIRECTIONAL_FILTER_SAMPLES 9
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
    //需要16个滤波样本
    #define DIRECTIONAL_FILTER_SAMPLES 16
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#if defined(_OTHER_PCF3)
    //需要4个滤波样本
    #define OTHER_FILTER_SAMPLES 4
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_OTHER_PCF5)
    //需要9个滤波样本
    #define OTHER_FILTER_SAMPLES 9
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_OTHER_PCF7)
    //需要16个滤波样本
    #define OTHER_FILTER_SAMPLES 16
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_SHADOWED_DIRECTIONAL_LIGHTS 4
#define MAX_CASCADE_COUNT 4
#define MAX_SHADOWED_OTHER_LIGHT_COUNT 16

//阴影图集
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);
TEXTURE2D_SHADOW(_OtherShadowAtlas);

CBUFFER_START(_CustomShadows)
    //级联数量和包围球数据
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    //级联数据
    float4 _CascadeData[MAX_CASCADE_COUNT];
    //阴影转换矩阵
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHTS * MAX_CASCADE_COUNT];
    //阴影过渡距离
    float4 _ShadowDistanceFade;
    float4 _ShadowAtlasSize;
    float4x4 _OtherShadowMatrices[MAX_SHADOWED_OTHER_LIGHT_COUNT];
    float4 _OtherShadowTiles[MAX_SHADOWED_OTHER_LIGHT_COUNT];
CBUFFER_END

struct ShadowMask
{
    bool always;
    bool distance;
    float4 shadows;
};

//表面的阴影数据
struct ShadowData
{
    int cascadeIndex;
    //是否采样阴影的标识
    float strength;
    //混合级联
    float cascadeBlend;
    ShadowMask shadowMask;
};

//方向光阴影的数据信息
struct DirectionalShadowData
{
    float strength;
    int tileIndex;
    //法线偏差
    float normalBias;
    int shadowMaskChannel;
};

struct OtherShadowData
{
    float strength;
    int tileIndex;
    int shadowMaskChannel;
    float3 lightPositionWS;
    float3 spotDirectionWS;
    bool isPoint;
    float3 lightDirectionWS;
};

static const float3 pointShadowPlanes[6] = {
    float3(-1.0,0.0,0.0),
    float3(1.0,0.0,0.0),
    float3(0.0,-1.0,0.0),
    float3(0.0,1.0,0.0),
    float3(0.0,0.0,-1.0),
    float3(0.0,0.0,1.0)
};

//计算阴影过渡时的强度
float FadedShadowStrength(float distance, float scale, float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}

//得到世界空间的表面阴影数据
ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData data;
    data.shadowMask.always = false;
    data.shadowMask.distance = false;
    data.shadowMask.shadows = 1.0;
    data.strength = FadedShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
    int i;
    //如果物体表面到球心的平方距离小于球体半径的平方，就说明该物体在这层级联包围球中，得到合适的级联层级索引
    for (i=0;i<_CascadeCount;i++)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
        if (distanceSqr < sphere.w)
        {
            //计算级联阴影的过渡强度
            float fade = FadedShadowStrength(distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z);
            //如果绘制的对象在最后一个级联的范围中，计算级联的过渡阴影强度，和阴影最大距离的过渡阴影强度相乘得到最终阴影强度
            if (i == _CascadeCount - 1)
            {
                data.strength *= fade;
            }else
            {
                data.cascadeBlend = fade;
            }
            break;
        }
    }
    //如果超出最后一个级联范围，标识符设置为0，不对阴影进行采样
    if (i==_CascadeCount && _CascadeCount > 0)
    {
        data.strength = 0.0;
    }
#if defined(_CASCADE_BLEND_DITHER)
    else if (data.cascadeBlend < surfaceWS.dither)
    {
        i+=1;
    }
#endif
#if !defined(_CASCADE_BLEND_SOFT)
    data.cascadeBlend = 1.0;
#endif
    data.cascadeIndex = i;
    return data;
}

float SampleOtherShadowAtlas(float3 positionSTS, float3 bounds)
{
    positionSTS.xy = clamp(positionSTS.xy, bounds.xy, bounds.xy + bounds.z);
    return SAMPLE_TEXTURE2D_SHADOW(_OtherShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterOtherShadow(float3 positionSTS, float3 bounds)
{
#if defined(OTHER_FILTER_SETUP)
    real weights[OTHER_FILTER_SAMPLES];
    real2 positions[OTHER_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.wwzz;
    OTHER_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i=0;i<OTHER_FILTER_SAMPLES;i++)
    {
        shadow += weights[i] * SampleOtherShadowAtlas(float3(positions[i].xy, positionSTS.z), bounds);
    }
    return shadow;
#else
    return SampleOtherShadowAtlas(positionSTS, bounds);
#endif
}

//得到非定向光源的实时阴影衰减
float GetOtherShadow(OtherShadowData other, ShadowData global, Surface surfaceWS)
{
    float tileIndex = other.tileIndex;
    float3 lightPlane = other.spotDirectionWS;
    if (other.isPoint)
    {
        float faceOffset = CubeMapFaceID(-other.lightDirectionWS);
        tileIndex += faceOffset;
        lightPlane = pointShadowPlanes[faceOffset];
    }
    float4 tileData = _OtherShadowTiles[tileIndex];
    float3 surfaceToLight = other.lightPositionWS - surfaceWS.position;
    float distanceToLightPlane = dot(surfaceToLight, lightPlane);
    float3 normalBias = surfaceWS.interpolatedNormal * (distanceToLightPlane * tileData.w);
    float4 positionSTS = mul(_OtherShadowMatrices[tileIndex], float4(surfaceWS.position + normalBias, 1.0f));
    //透视投影，变换位置XYZ除以Z
    return FilterOtherShadow(positionSTS.xyz / positionSTS.w, tileData.xyz);
}

//采样阴影图集
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

//PCF滤波采样平行光阴影
float FilterDirectionalShadow(float3 positionSTS)
{
#if defined(DIRECTIONAL_FILTER_SETUP)
    //样本权重
    float weights[DIRECTIONAL_FILTER_SAMPLES];
    //样本位置
    float2 positions[DIRECTIONAL_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.yyxx;
    DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i=0;i<DIRECTIONAL_FILTER_SAMPLES;i++)
    {
        //遍历所有样本得到权重和
        shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
    }
    return shadow;
#else
    return SampleDirectionalShadowAtlas(positionSTS);
#endif
}

float GetCascadedShadow(DirectionalShadowData directional, ShadowData global, Surface surfaceWS)
{
    //计算法线偏差
    float3 normalBias = surfaceWS.interpolatedNormal * directional.normalBias * _CascadeData[global.cascadeIndex].y;
    //通过加上法线偏移后的表面顶点位置得到在阴影纹理空间的新位置，然后对图集进行采样
    float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex], float4(surfaceWS.position + normalBias, 1.0f)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);
    //如果级联混合小于1代表在
    if (global.cascadeBlend < 1.0)
    {
        normalBias = surfaceWS.interpolatedNormal * directional.normalBias * _CascadeData[global.cascadeIndex + 1].y;
        positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1], float4(surfaceWS.position + normalBias, 1.0f)).xyz;
        shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadeBlend);
    }
    return shadow;
}

//得到烘焙阴影的衰减值
float GetBakedShadow(ShadowMask mask, int channel)
{
    float shadow = 1.0;
    if (mask.always || mask.distance)
    {
        if (channel >= 0)
        {
            shadow = mask.shadows[channel];
        }
    }
    return shadow;
}

float GetBakedShadow(ShadowMask mask, int channel, float strength)
{
    if (mask.always || mask.distance)
    {
        return lerp(1.0, GetBakedShadow(mask, channel), strength);
    }
    return 1.0;
}

//混合烘焙和实时阴影
float MixBakedAndRealtimeShadows(ShadowData global, float shadow, int shadowMaskChannel, float strength)
{
    float baked = GetBakedShadow(global.shadowMask, shadowMaskChannel);
    if (global.shadowMask.always)
    {
        shadow = lerp(1.0, shadow, global.strength);
        shadow = min(baked, shadow);
        return lerp(1.0, shadow, strength);
    }
    if (global.shadowMask.distance)
    {
        shadow = lerp(baked, shadow, global.strength);
        return lerp(1.0, shadow, strength);
    }
    return lerp(1.0, shadow, strength * global.strength);
}

//计算阴影衰减
float GetDirectionalShadowAttenuation(DirectionalShadowData directional, ShadowData global, Surface surfaceWS)
{
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif
    float shadow;
    if (directional.strength * global.strength <= 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask, directional.shadowMaskChannel,abs(directional.strength));
    }else
    {
        shadow = GetCascadedShadow(directional, global, surfaceWS);
        //阴影混合
        shadow = MixBakedAndRealtimeShadows(global, shadow, directional.shadowMaskChannel, directional.strength);
    }
    
    return shadow;
}

float GetOtherShadowAttenuation(OtherShadowData other, ShadowData global, Surface surfaceWS)
{
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif

    float shadow;
    if (other.strength * global.strength <= 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask, other.shadowMaskChannel, abs(other.strength));
    }else
    {
        shadow = GetOtherShadow(other, global, surfaceWS);
        shadow = MixBakedAndRealtimeShadows(global,shadow,other.shadowMaskChannel, other.strength);
    }
    return shadow;
}


#endif