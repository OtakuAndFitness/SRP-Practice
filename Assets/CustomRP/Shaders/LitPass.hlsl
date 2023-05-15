#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    // float4 _BaseColor;
CBUFFER_END

struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float3 normalOS : TEXCOORD3;
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD4;
    float2 uv : TEXCOORD0;
    float2 detailUV : TEXCOORD2;
    float3 normalWS : TEXCOORD3;
    GI_VARYINGS_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    TRANSFER_GI_DATA(input,output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.uv = TransformBaseUV(input.uv);
    output.detailUV = TransformDetailUV(input.uv);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    return output;
}

float4 LitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    ClipLOD(input.positionCS, unity_LODFade.x);
    float4 baseCol = GetBase(input.uv);

#if defined(_CLIPPING)
    clip(baseCol.a - GetCutOff());
#endif

    Surface sf;
    sf.position = input.positionWS;
    sf.normal = normalize(input.normalWS);
    sf.viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
    //获取表面深度
    sf.depth = -TransformWorldToView(input.positionWS).z;
    sf.color = baseCol.rgb;
    sf.alpha = baseCol.a;
    sf.metallic = GetMetallic(input.uv);
    sf.smoothness = GetSmoothness(input.uv);
    sf.occlusion = GetOcclusion(input.uv);
    sf.fresnelStrength = GetFresnel();
    //计算抖动值
    sf.dither = InterleavedGradientNoise(input.positionCS.xy,0);
#if defined(_PREMULTIPY_ALPHA)
    BRDF brdf = GetBRDF(sf,true);
#else
    BRDF brdf = GetBRDF(sf);
#endif
    GI gi = GetGI(GI_FRAGMENT_DATA(input), sf, brdf);
    float3 color = GetLighting(sf,brdf, gi);
    color += GetEmission(input.uv);
    return float4(color,sf.alpha);
}

#endif