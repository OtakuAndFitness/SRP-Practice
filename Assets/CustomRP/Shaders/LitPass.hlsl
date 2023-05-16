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
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float2 uv : VAR_BASE_UV;
#if defined(_DETAIL_MAP)
    float2 detailUV : VAR_DETAIL_UV;
#endif
    float3 normalWS : VAR_NORMAL;
#if defined(_NORMAL_MAP)
    float4 tangentWS : VAR_TANGENT;
#endif
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
#if defined(_DETAIL_MAP)
    output.detailUV = TransformDetailUV(input.uv);
#endif
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
#if defined(_NORMAL_MAP)
    output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz),input.tangentOS.w);
#endif
    return output;
}

float4 LitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    ClipLOD(input.positionCS, unity_LODFade.x);
    InputConfig config = GetInputConfig(input.uv);
#if defined(_MASK_MAP)
    config.useMask = true;
#endif
#if defined(_DETAIL_MAP)
    config.detailUV = input.detailUV;
    config.useDetail = true;
#endif

    float4 baseCol = GetBase(config);

#if defined(_CLIPPING)
    clip(baseCol.a - GetCutOff());
#endif

    Surface sf;
    sf.position = input.positionWS;
#if defined(_NORMAL_MAP)
    sf.normal = NormalTangentToWorld(GetNormalTS(config), input.normalWS, input.tangentWS);
    sf.interpolatedNormal = input.normalWS;
#else
    sf.normal = normalize(input.normalWS);
    sf.interpolatedNormal = sf.normal;
#endif
    
    sf.viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
    //获取表面深度
    sf.depth = -TransformWorldToView(input.positionWS).z;
    sf.color = baseCol.rgb;
    sf.alpha = baseCol.a;
    sf.metallic = GetMetallic(config);
    sf.smoothness = GetSmoothness(config);
    sf.occlusion = GetOcclusion(config);
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
    color += GetEmission(config);
    return float4(color,sf.alpha);
}

#endif