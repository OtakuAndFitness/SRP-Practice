Shader "Custom/Unlit"
{
    Properties
    {
        [HDR] _BaseColor("Base Color",Color) = (1.0,1.0,1.0,1.0)
        _BaseMap("Main Texture", 2D) = "white"{}
        _CutOff("Cut Off",Range(0.0,1.0)) = 0.5
        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0 
        //设置混合模式
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
        //默认写入深度
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 1

    }
    SubShader
    {
        
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "UnlitInput.hlsl"
        ENDHLSL

        Pass
        {
            //定义混合模式
            Blend [_SrcBlend][_DstBlend], One OneMinusSrcAlpha
            //是否写入深度
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature _CLIPPING
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #pragma multi_compile_instancing
            #include "UnlitPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Tags{"LightMode"="ShadowCaster"}
            
            ColorMask 0
            
            HLSLPROGRAM
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
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
