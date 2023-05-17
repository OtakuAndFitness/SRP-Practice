using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{ 
    const string cmbName = "Shadows";

    CommandBuffer cmb = new CommandBuffer()
    {
        name = cmbName
    };

    //可投射阴影的平行光数量
    const int maxShadowedDirectionalLightCount = 4, maxCascades = 4, maxShadowedOtherLightCount = 16;//最大级联数量

    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
        //斜度比例偏差值
        public float slopeScaleBias;
        //阴影视锥体近裁剪平面偏移
        public float nearPlaneOffset;
    }

    struct ShadowedOtherLight
    {
        public int visibleLightIndex;
        public float slopeScaleBias;
        public float normalBias;
    }
    
    //PCF滤波模式
    static string[] directionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };
    
    static string[] cascadeBlendKeywords =
    {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };

    static string[] shadowMaskKeywords =
    {
        "_SHADOW_MASK_ALWAYS",
        "_SHADOW_MASK_DISTANCE"
    };
    
    //非定向光源的滤波模式
    static string[] otherFilterKeywords =
    {
        "_OTHER_PCF3",
        "_OTHER_PCF5",
        "_OTHER_PCF7",
    };
    
    bool useShadowMask;

    //储存可投射阴影的可见光源的索引
    ShadowedDirectionalLight[] shadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    ShadowedOtherLight[] shadowedOtherLights =
        new ShadowedOtherLight[maxShadowedOtherLightCount];

    //已储存的可投射阴影的平行光数量
    int shadowedDirectionalLightCount, shadowedOtherLightCount;

    static int
        dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),

        dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),

        shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade"),

        shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize"),

        cascadeCountId = Shader.PropertyToID("_CascadeCount"),

        cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
        //级联数据
        cascadeDataId = Shader.PropertyToID("_CascadeData"),
        otherShadowAtlasId = Shader.PropertyToID("_OtherShadowAtlas"),
        otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices");
    
    //光源的阴影转换矩阵
    static Matrix4x4[] 
        dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades],
        otherShadowMatrices = new Matrix4x4[maxShadowedOtherLightCount];
    

    static Vector4[] 
        cascadeCullingSpheres = new Vector4[maxCascades],

        cascadeData = new Vector4[maxCascades];
    
    Vector4 atlasSizes;
    
    ScriptableRenderContext context;
    ShadowSettings shadowSettings;
    CullingResults crs;

    public void Setup(ScriptableRenderContext context, CullingResults crs, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.crs = crs;
        this.shadowSettings = shadowSettings;
        shadowedDirectionalLightCount = 0;
        shadowedOtherLightCount = 0;
        useShadowMask = false;
        // cmb.BeginSample(cmbName);
        // SetUpDirectionalLight();
        // SetupLights();
        // cmb.EndSample(cmbName);
        // context.ExecuteCommandBuffer(cmb);
        // cmb.Clear();
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }

    public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        //储存可见光源索引，前提是光源开启了阴影投射并且阴影强度大于0
        if (shadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None &&
            //还需要加上一个判断，是否在阴影最大投射距离内，有被该光源影响且需要投影的物体存在，如果没有就不需要渲染该光源的阴影贴图了
            light.shadowStrength > 0f)
        {
            float maskChannel = -1;
            //如果使用了shadowmask
            LightBakingOutput lightBaking = light.bakingOutput;
            if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
                lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel;
            }
            if (!crs.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds))
            {
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
            }
            shadowedDirectionalLights[shadowedDirectionalLightCount] = new ShadowedDirectionalLight
            {
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias,
                nearPlaneOffset = light.shadowNearPlane
            };
            //返回阴影强度和阴影图块的偏移
            return new Vector4(light.shadowStrength, shadowSettings.directional.cascadeCount * shadowedDirectionalLightCount++, light.shadowNormalBias, maskChannel);
        }

        return new Vector4(0f, 0f, 0f, -1f);
    }

    public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
    {
        if (light.shadows == LightShadows.None || light.shadowStrength <= 0f)
        {
            return new Vector4(0f, 0f, 0f, -1f);
        }
        float maskChannel = -1;
        LightBakingOutput lightBaking = light.bakingOutput;
        if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
            lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
        {
            useShadowMask = true;
            maskChannel = lightBaking.occlusionMaskChannel;
        }
        if (shadowedOtherLightCount >= maxShadowedOtherLightCount || !crs.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds))
        {
            return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
        }

        shadowedOtherLights[shadowedOtherLightCount] = new ShadowedOtherLight
        {
            visibleLightIndex = visibleLightIndex,
            slopeScaleBias = light.shadowBias,
            normalBias = light.shadowNormalBias
        };
        
        return new Vector4(light.shadowStrength, shadowedOtherLightCount++, 0f, maskChannel);

    }

    public void Render()
    {
        if (shadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            cmb.GetTemporaryRT(dirShadowAtlasId, 1,1,32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
        
        if (shadowedOtherLightCount > 0)
        {
            RenderOtherShadows();
        }
        else
        {
            cmb.SetGlobalTexture(otherShadowAtlasId, dirShadowAtlasId);
        }
        
        //是否使用阴影模板
        cmb.BeginSample(cmbName);
        SetKeywords(shadowMaskKeywords, useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);
        cmb.EndSample(cmbName);
        
        //将级联数量和包围球数据发送到GPU
        cmb.SetGlobalInt(cascadeCountId, shadowSettings.directional.cascadeCount);
        //阴影距离过渡相关数据发送GPU
        float f = 1f - shadowSettings.directional.cascadeFade;
        cmb.SetGlobalVector(shadowDistanceFadeId, new Vector4(1f / shadowSettings.maxDistance, 1f / shadowSettings.distanceFade, 1f / (1f - f * f)));
        //传递图集大小和纹素大小
        cmb.SetGlobalVector(shadowAtlasSizeId, atlasSizes);
        cmb.EndSample(cmbName);
        ExecuteBuffer();
    }

    void RenderOtherShadows()
    {
        //创建rt, 并指定该类型是阴影贴图
        int atlasSize = (int)shadowSettings.other.atlasSize;
        atlasSizes.x = atlasSize;
        atlasSizes.y = 1f / atlasSize;
        
        cmb.GetTemporaryRT(otherShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        cmb.SetRenderTarget(otherShadowAtlasId, RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        cmb.ClearRenderTarget(true,false,Color.clear);
        
        cmb.BeginSample(cmbName);
        ExecuteBuffer();
        //要分割的图块大小和数量
        int tiles = shadowedOtherLightCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < shadowedDirectionalLightCount; i++)
        {
            RenderSpotShadows(i, split, tileSize);
        }
        //阴影转换矩阵传入GPU
        cmb.SetGlobalMatrixArray(otherShadowMatricesId, otherShadowMatrices);
        SetKeywords(otherFilterKeywords, (int)shadowSettings.other.filter - 1);
        cmb.EndSample(cmbName);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows()
    {
        //创建rt, 并指定该类型是阴影贴图
        int atlasSize = (int)shadowSettings.directional.atlasSize;
        atlasSizes.x = atlasSize;
        atlasSizes.y = 1f / atlasSize;
        
        cmb.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        cmb.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        cmb.ClearRenderTarget(true,false,Color.clear);
        
        cmb.BeginSample(cmbName);
        ExecuteBuffer();
        //要分割的图块大小和数量
        int tiles = shadowedDirectionalLightCount * shadowSettings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        cmb.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
        //级联数据发送GPU
        cmb.SetGlobalVectorArray(cascadeDataId, cascadeData);
        //阴影转换矩阵传入GPU
        cmb.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        //设置关键字
        SetKeywords(directionalFilterKeywords, (int)shadowSettings.directional.filter - 1);
        SetKeywords(cascadeBlendKeywords, (int)shadowSettings.directional.cascadeBlend - 1);
        cmb.EndSample(cmbName);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(crs, light.visibleLightIndex);
        //得到级联阴影贴图需要的参数
        int cascadeCount = shadowSettings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = shadowSettings.directional.CascadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - shadowSettings.directional.cascadeFade);
        for (int i = 0; i < cascadeCount; i++)
        {
            crs.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i, cascadeCount,ratios, tileSize, light.nearPlaneOffset,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData shadowSplitData);
            //得到第一个光源的级联包围球数据
            if (index == 0)
            {
                //设置级联数据
                SetCascadeData(i, shadowSplitData.cullingSphere, tileSize);

            }
            //剔除偏差
            shadowSplitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowDrawingSettings.splitData = shadowSplitData;
            //调整图块索引，它等于光源的图块偏移加上级联的索引
            int tileIndex = tileOffset + i;
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewPort(tileIndex, split, tileSize), split);
            //设置视图投影矩阵
            cmb.SetViewProjectionMatrices(viewMatrix,projectionMatrix);
            cmb.SetGlobalDepthBias(0,light.slopeScaleBias);
            //绘制阴影
            ExecuteBuffer();
            context.DrawShadows(ref shadowDrawingSettings);
            cmb.SetGlobalDepthBias(0f,0f);
        }
    }
    
    //渲染聚光灯阴影
    void RenderSpotShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];
        var shadowDrawingSettings = new ShadowDrawingSettings(crs, light.visibleLightIndex);
        crs.ComputeSpotShadowMatricesAndCullingPrimitives(light.visibleLightIndex, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData shadowSplitData);
        shadowDrawingSettings.splitData = shadowSplitData;
        otherShadowMatrices[index] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewPort(index, split, tileSize), split);
        //设置视图投影矩阵
        cmb.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        //设置斜度比例偏差
        cmb.SetGlobalDepthBias(0f, light.slopeScaleBias);
        //绘制阴影
        ExecuteBuffer();
        context.DrawShadows(ref shadowDrawingSettings);
        cmb.SetGlobalDepthBias(0f,0f);
    }
    
    //设置级联数据
    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        //包围球直径除以阴影图块尺寸=纹素大小
        float texelSize = 2f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)shadowSettings.directional.filter + 1f);
        
        cullingSphere.w -= filterSize;
        //得到半径的平方值
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
        cascadeData[index] = new Vector4(1f / cullingSphere.w, texelSize * 1.4142136f);
    }

    //调整渲染视口来渲染单个图块
    Vector2 SetTileViewPort(int index, int split, float tileSize)
    {
        //计算图块索引的偏移量
        Vector2 offset = new Vector2(index % split, index / split);
        //设置渲染视口，拆分多个图块
        cmb.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }
    
    //返回一个从世界空间到阴影图块空间的转换矩阵
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        //如果使用了反向Zbuffer
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        //设置矩阵坐标
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        
        return m;
    }
    
    //设置关键字开启哪种PCF滤波模式
    void SetKeywords(string[] keywords, int enableIndex)
    {
        // int enableIndex = (int)(shadowSettings.directional.filter - 1);
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i == enableIndex)
            {
                cmb.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                cmb.DisableShaderKeyword(keywords[i]);
            }
        }
    }

    public void Cleanup()
    {
        cmb.ReleaseTemporaryRT(dirShadowAtlasId);
        if (shadowedOtherLightCount > 0)
        {
            cmb.ReleaseTemporaryRT(otherShadowAtlasId);
        }
        ExecuteBuffer();
    }
}

