using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShadowSettings
{
    //阴影最大距离
    [Min(0f)]
    public float maxDistance = 100f;
    
    //阴影过渡距离
    [Range(0.001f, 1f)]
    public float distanceFade = 0.1f;
    
    public enum TextureSize
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192
    }
    
    [Serializable]
    public struct Directional
    {
        public TextureSize atlasSize;

        public FilterMode filter;
        //级联数量
        [Range(1,4)]
        public int cascadeCount;
        //级联比例
        [Range(0f,1f)]
        public float cascadeRatio1, cascadeRatio2, cascadeRatio3;

        public Vector3 CascadeRatios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);
        
        //级联淡入值
        [Range(0.001F, 1F)]
        public float cascadeFade;
        
        public CascadeBlendMode cascadeBlend;
        public enum CascadeBlendMode
        {
            Hard,
            Soft,
            Dither
        }

    }
    
    //PCF滤波模式
    public enum FilterMode
    {
        PCF2x2,
        PCF3x3,
        PCF5x5,
        PCF7x7
    }


    public Directional directional = new Directional
    {
        atlasSize = TextureSize._1024,
        filter = FilterMode.PCF2x2,
        cascadeBlend = Directional.CascadeBlendMode.Hard,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f,
        cascadeRatio3 = 0.5f,
        cascadeFade = 0.1f
    };
    
}
