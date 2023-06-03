#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED


struct Attributes
{
    float3 positionOS : POSITION;
    float4 color : COLOR;
#if defined(_FLIPBOOK_BLENDING)
    float4 uv : TEXCOORD0;
    float flipbookBlend : TEXCOORD1;
#else
    float2 uv : TEXCOORD0;
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS_SS : SV_POSITION;
    float2 uv : TEXCOORD0;
#if defined(_VERTEX_COLORS)
    float4 color : VAR_COLOR;
#endif
#if defined(_FLIPBOOK_BLENDING)
    float3 flipbookUVB : VAR_FLIPBOOK;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(positionWS);
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseMap_ST);
    output.uv.xy = TransformBaseUV(input.uv.xy);
#if defined(_FLIPBOOK_BLENDING)
    output.flipbookUVB.xy = TransformBaseUV(input.uv.zw);
    output.flipbookUVB.z = input.flipbookBlend;
#endif
#if defined(_VERTEX_COLORS)
    output.color = input.color;
#endif
    return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GetInputConfig(input.positionCS_SS, input.uv);
#if defined(_VERTEX_COLORS)
    config.color = input.color;
#endif
#if defined(_FLIPBOOK_BLENDING)
    config.flipbookUVB = input.flipbookUVB;
    config.flipbookBlending = true;
#endif
#if defined(_NEAR_FADE)
    config.nearFade = true;
#endif
#if defined(_SOFT_PARTICLES)
    config.softParticles = true;
#endif
    
    float4 baseCol = GetBase(config);

    #if defined(_CLIPPING)
        clip(baseCol.a - GetCutOff());
    #endif

    
    return float4(baseCol.rgb, GetFinalAlpha(baseCol.a));
}

#endif