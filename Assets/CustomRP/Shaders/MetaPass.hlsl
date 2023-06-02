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
    float4 positionCS_SS : SV_POSITION;
    float2 uv : VAR_BASE_UV;
};

bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

Varyings MetaPassVertex(Attributes input)
{
    Varyings output;
    input.positionOS.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
    output.positionCS_SS = TransformWorldToHClip(input.positionOS);
    output.uv = TransformBaseUV(input.uv);
    return output;
}

float4 MetaPassFragment(Varyings input) : SV_TARGET
{
    InputConfig config = GetInputConfig(input.positionCS_SS,input.uv);
    float4 base = GetBase(config);
    Surface sf;
    ZERO_INITIALIZE(Surface, sf);
    sf.color = base.rgb;
    sf.metallic = GetMetallic(config);
    sf.smoothness = GetSmoothness(config);
    BRDF brdf = GetBRDF(sf);
    float4 meta = 0.0;
    if (unity_MetaFragmentControl.x)
    {
        //漫反射率
        meta = float4(brdf.diffuse,1.0);
        meta.rgb += brdf.specular * brdf.roughness * 0.5;
        meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
    }else if (unity_MetaFragmentControl.y)
    {
        //自发光
        meta = float4(GetEmission(config), 1.0);
    }
    return meta;
}

#endif