#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHTS 4

CBUFFER_START(_CustomLight)
    int _DirectionalLightCounts;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHTS];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHTS];
CBUFFER_END

struct Light
{
    float3 color;
    float3 direction;
};

int GetLightCounts()
{
    return _DirectionalLightCounts;
}

Light GetDirectionalLight(int index)
{
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    return light;
}



#endif