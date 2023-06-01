#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

struct Surface
{
    float3 normal;
    float3 interpolatedNormal;
    float3 color;
    float alpha;
    float metallic;
    float smoothness;
    float occlusion;
    float3 viewDir;

    //表面位置
    float3 position;
    //表面深度
    float depth;
    float dither;

    //菲涅尔反射强度
    float fresnelStrength;

    uint renderingLayerMask;
};



#endif