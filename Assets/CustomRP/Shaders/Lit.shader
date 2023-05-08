Shader "Custom/Lit"
{
    Properties
    {
        _BaseColor("Base Color",Color) = (0.5,0.5,0.5,1.0)
        _BaseMap("Main Texture", 2D) = "white"{}
        _CutOff("Cut Off",Range(0.0,1.0)) = 0.5
        [Toggle()] _Clipping("Alpha Clipping", Float) = 0
        [Toggle(_PREMULTIPY_ALPHA)] _PremultipyAlpha("Premultipy Alpha", Float) = 0
        _Metallic ("_Metallic", Range(0,1)) = 0
        _Smoothness ("_Smoothness", Range(0,1)) = 0.5 
        //设置混合模式
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
        //默认写入深度
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 1
        //阴影模式
        [KeywordEnum(On, Clip, Dither, Off)] _Shadows("Shadows", Float) = 0
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows("Receive Shadows", Float) = 1

    }
    SubShader
    {

        Pass
        {
            Tags{"LightMode"="CustomLight"}
            //定义混合模式
            Blend [_SrcBlend][_DstBlend]
            //是否写入深度
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _RECEIVE_SHADOWS
            #pragma shader_feature _PREMULTIPY_ALPHA
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #pragma multi_compile_instancing
            #pragma target 3.5//排除OpenGL ES 2.0
            #include "LitPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Tags{"LightMode"="ShadowCaster"}
            
            ColorMask 0
            
            HLSLPROGRAM
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            #pragma shader_feature _PREMULTIPY_ALPHA
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile_instancing
            #pragma target 3.5//排除OpenGL ES 2.0
            #include "ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "CustomShaderGUI"
}
