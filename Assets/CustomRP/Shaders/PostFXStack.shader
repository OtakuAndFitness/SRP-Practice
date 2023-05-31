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
            Name "Bloom Add"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomAddPassFragment
            ENDHLSL
        }
        
        Pass{
            Name "Bloom Scatter"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomScatterPassFragment
            ENDHLSL
        }
        
        Pass{
            Name "Bloom Scatter Final"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomScatterFinalPassFragment
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
            Name "Color Grading None"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ColorGradingNonePassFragment
            ENDHLSL
        }
        
        Pass{
            Name "Color Grading ACES"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ColorGradingACESPassFragment
            ENDHLSL
        }
        
        Pass{
            Name "Color Grading Neutral"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ColorGradingNeutralPassFragment
            ENDHLSL
        }
        
        Pass{
            Name "Color Grading Reinhard"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ColorGradingReinhardPassFragment
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
        
        Pass{
            Name "Final"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment FinalPassFragment
            ENDHLSL
        }
        
        
    }
//    CustomEditor "CustomShaderGUI"

}
