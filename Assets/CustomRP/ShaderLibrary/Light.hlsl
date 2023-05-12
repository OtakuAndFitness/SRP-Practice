#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHTS 4

CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHTS];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHTS];
    //阴影数据
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHTS];
CBUFFER_END

struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};

//获取方向光的阴影数据
DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    //获取灯光的法线偏差值
    data.normalBias = _DirectionalLightShadowData[lightIndex].z;
    data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
    return data;
}

int GetDirLightCount()
{
    return _DirectionalLightCount;
}

Light GetDirectionalLight(int index, Surface surfaceWS, ShadowData shadowData)
{
    Light light;
    //得到阴影数据
    DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    //得到阴影衰减
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
    return light;
}



#endif