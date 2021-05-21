Shader "Custom/Lit"
{
    Properties
    {
        _BaseColor("Base Color",Color) = (0.5,0.5,0.5,1.0)
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

        Pass
        {
            Tags{"LightMode"="CustomLight"}
            //定义混合模式
            Blend [_SrcBlend][_DstBlend]
            //是否写入深度
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma shader_feature _CLIPPING
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #pragma multi_compile_instancing
            #pragma Target 3.5//排除OpenGL ES 2.0
            #include "LitPass.hlsl"
            ENDHLSL
        }
    }
}
