#ifndef CUSTOM_META_PASS_INCLUDED
#define CUSTOM_META_PASS_INCLUDED

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"


struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float2 lightMapUV : TEXCOORD1;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

Varyings MetaPassVertex(Attributes input)
{
    Varyings output;
    input.positionOS.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
    output.positionCS = TransformWorldToHClip(input.positionOS);
    output.uv = TransformBaseUV(input.uv);
    return output;
}

float4 MetaPassFragment(Varyings input) : SV_TARGET
{
    float4 base = GetBase(input.uv);
    Surface sf;
    ZERO_INITIALIZE(Surface, sf);
    sf.color = base.rgb;
    sf.metallic = GetMetallic();
    sf.smoothness = GetSmoothness();
    BRDF brdf = GetBRDF(sf);
    float4 meta = 0.0;
    if (unity_MetaFragmentControl.x)
    {
        meta = float4(brdf.diffuse,1.0);
        meta.rgb += brdf.specular * brdf.roughness * 0.5;
        meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
    }else if (unity_MetaFragmentControl.y)
    {
        meta = float4(GetEmission(input.uv), 1.0);
    }
    return meta;
}

#endif