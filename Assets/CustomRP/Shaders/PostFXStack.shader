Shader "Hidden/Custom/Post FX Stack"
{
//    Properties
//    {
//        [HDR] _BaseColor("Base Color",Color) = (1.0,1.0,1.0,1.0)
//        _BaseMap("Main Texture", 2D) = "white"{}
//        _CutOff("Cut Off",Range(0.0,1.0)) = 0.5
//        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0 
//        //设置混合模式
//        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
//        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
//        //默认写入深度
//        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 1
//
//    }
    
    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off
        
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "PostFXStackPasses.hlsl"
        ENDHLSL
        
        Pass{
            Name "Bloom Horizontal"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomHorizontalPassFragment
            ENDHLSL
        }
        
        Pass{
            Name "Bloom Vertical"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomVerticalPassFragment
            ENDHLSL
        }
        
        Pass{
            Name "Bloom Combine"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomCombinePassFragment
            ENDHLSL
        }
        
        Pass{
            Name "Bloom Prefilter"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomPrefilterPassFragment
            ENDHLSL
        }
        
         Pass{
            Name "Bloom Prefilter Fireflies"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomPrefilterFirefliesPassFragment
            ENDHLSL
        }
        
        Pass{
            Name "Copy"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment CopyPassFragment
            ENDHLSL
        }
        
        
    }
//    CustomEditor "CustomShaderGUI"

}
