#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHTS 4

CBUFFER_START(_CustomLight)
    int _DirectionalLightCounts;
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
DirectionalShadowData GetDirectionalShadowData(int lightIndex)
{
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y;
    return data;
}

int GetLightCounts()
{
    return _DirectionalLightCounts;
}

Light GetDirectionalLight(int index, Surface surfaceWS)
{
    Light light;
    //得到阴影数据
    DirectionalShadowData shadowData = GetDirectionalShadowData(index);
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    //得到阴影衰减
    light.attenuation = GetDirectionalShadowAttenuation(shadowData, surfaceWS);
    return light;
}



#endif