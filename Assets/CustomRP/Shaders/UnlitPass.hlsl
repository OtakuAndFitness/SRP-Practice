#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED


struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseMap_ST);
    output.uv = input.uv * baseST.xy + baseST.zw;
    return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 baseCol = GetBase(input.uv);
   

    #if defined(_CLIPPING)
        clip(baseCol.a - GetCutOff());
    #endif

    
    return baseCol;
}

#endif