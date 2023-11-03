Shader "Custom/Lit"
{
    Properties
    {
        _BaseColor("Base Color",Color) = (0.5,0.5,0.5,1.0)
        _BaseMap("Main Texture", 2D) = "white"{}
        _CutOff("Cut Off",Range(0.0,1.0)) = 0.5
        [Toggle()] _Clipping("Alpha Clipping", Float) = 0
        [Toggle(_PREMULTIPY_ALPHA)] _PremultipyAlpha("Premultipy Alpha", Float) = 0
        [Toggle (_MASK_MAP)] _MaskMapToggle("Mask Map", Float) = 0
        [NoScaleOffset] _MaskMap("Mask Map", 2D) = "white"{}
        _Metallic ("_Metallic", Range(0,1)) = 0
        _Smoothness ("_Smoothness", Range(0,1)) = 0.5
        _Occlusion ("Occlusion", Range(0,1)) = 1
        _Fresnel("_Fresnel", Range(0,1)) = 1
        [Toggle (_NORMAL_MAP)] _NormalMapToggle("Normal Map", Float) = 0
        [NoScaleOffset] _NormalMap("Normal Map", 2D) = "bump"{}
        _NormalScale("Normal Scale", Range(0,1)) = 1
        //设置混合模式
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
        //默认写入深度
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 1
        //阴影模式
        [KeywordEnum(On, Clip, Dither, Off)] _Shadows("Shadows", Float) = 0
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows("Receive Shadows", Float) = 1
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "white"{}
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,0)
        [Toggle (_DETAIL_MAP)] _DetailMapToggle("Detail Map", Float) = 0
        _DetailMap("Details", 2D) = "linearGrey"{}
        [NoScaleOffset]_DetailNormalMap("Detail Normals", 2D) = "bump"{}
        _DetailNormalScale("Detail Normal Scale", Range(0,1)) = 1
        _DetailAlbedo("Detail Albedo", Range(0,1)) = 1
        _DetailSmoothness("Detail Smoothness", Range(0,1)) = 1
        //For transparent bake
        [HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
        [HideInInspector] _Color("Color for Lightmap", Color) = (0.5,0.5,0.5,1)

    }
    SubShader
    {
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "LightInput.hlsl"
        ENDHLSL

        Pass
        {
            Tags{"LightMode"="CustomLit"}
            //定义混合模式
            Blend [_SrcBlend][_DstBlend], One OneMinusSrcAlpha
            //是否写入深度
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _NORMAL_MAP
            #pragma shader_feature _MASK_MAP
            #pragma shader_feature _DETAIL_MAP
            #pragma shader_feature _RECEIVE_SHADOWS
            #pragma shader_feature _PREMULTIPY_ALPHA
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _OTHER_PCF3 _OTHER_PCF5 _OTHER_PCF7
            #pragma multi_compile _ _LIGHTS_PER_OBJECT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile _ _SHADOW_MASK_ALWAYS _SHADOW_MASK_DISTANCE
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
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
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #pragma target 3.5//排除OpenGL ES 2.0
            #include "ShadowCasterPass.hlsl"
            ENDHLSL
        }
        
        Pass{
            Tags{"LightMode" = "Meta"}
            
            Cull Off
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment
            #include "MetaPass.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "CustomShaderGUI"
}
