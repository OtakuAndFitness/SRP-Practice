using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class Shadows
{ 
    // const string cmbName = "Shadows";

    // CommandBuffer cmb = new CommandBuffer()
    // {
    //     name = cmbName
    // };

    private CommandBuffer _buffer;

    //可投射阴影的平行光数量, 最大级联数量, 可投射阴影的其他光源数量
    const int maxShadowedDirectionalLightCount = 4, maxCascades = 4, maxShadowedOtherLightCount = 16;

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
        public bool isPoint;
    }
    
    //PCF滤波模式
    static readonly GlobalKeyword[] 
        directionalFilterKeywords =
    {
        GlobalKeyword.Create("_DIRECTIONAL_PCF3"),
        GlobalKeyword.Create("_DIRECTIONAL_PCF5"),
        GlobalKeyword.Create("_DIRECTIONAL_PCF7"),
    };
    
    static readonly GlobalKeyword[] cascadeBlendKeywords =
    {
        GlobalKeyword.Create("_CASCADE_BLEND_SOFT"),
        GlobalKeyword.Create("_CASCADE_BLEND_DITHER")
    };

    static readonly GlobalKeyword[] shadowMaskKeywords =
    {
        GlobalKeyword.Create("_SHADOW_MASK_ALWAYS"),
        GlobalKeyword.Create("_SHADOW_MASK_DISTANCE")
    };
    
    //非定向光源的滤波模式
    static readonly GlobalKeyword[] otherFilterKeywords =
    {
        GlobalKeyword.Create("_OTHER_PCF3"),
        GlobalKeyword.Create("_OTHER_PCF5"),
            GlobalKeyword.Create("_OTHER_PCF7"),
    };
    
    bool useShadowMask;

    //储存可投射阴影的可见光源的索引
    ShadowedDirectionalLight[] shadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    ShadowedOtherLight[] shadowedOtherLights =
        new ShadowedOtherLight[maxShadowedOtherLightCount];

    //已储存的可投射阴影的平行光数量， 已储存的可投射阴影的其他光源数量
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
        otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices"),
        shadowPancakingId = Shader.PropertyToID("_ShadowPancaking"),
        otherShadowTilesId = Shader.PropertyToID("_OtherShadowTiles");
    
    //光源的阴影转换矩阵
    static Matrix4x4[] 
        dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades],
        otherShadowMatrices = new Matrix4x4[maxShadowedOtherLightCount];
    

    static Vector4[] 
        cascadeCullingSpheres = new Vector4[maxCascades],

        cascadeData = new Vector4[maxCascades],
        otherShadowTiles = new Vector4[maxShadowedOtherLightCount];
    
    Vector4 atlasSizes;
    
    ScriptableRenderContext context;
    ShadowSettings shadowSettings;
    CullingResults crs;

    private TextureHandle directionalAtlas, otherAtlas;

    public void Setup(CullingResults crs, ShadowSettings shadowSettings)
    {
        // _buffer = context.cmd;
        // this.context = context.renderContext;
        this.crs = crs;
        this.shadowSettings = shadowSettings;
        shadowedDirectionalLightCount = shadowedOtherLightCount = 0;
        useShadowMask = false;
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        //储存可见光源索引，前提是光源开启了阴影投射并且阴影强度大于0
        if (shadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None &&
            //还需要加上一个判断，是否在阴影最大投射距离内，有被该光源影响且需要投影的物体存在，如果没有就不需要渲染该光源的阴影贴图了
            light.shadowStrength > 0f)
        {
            float maskChannel = -1;
            LightBakingOutput lightBaking = light.bakingOutput;
            //如果使用了shadowmask
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
        bool isPoint = light.type == LightType.Point;
        int newLightCount = shadowedOtherLightCount + (isPoint ? 6 : 1);
        if (newLightCount >= maxShadowedOtherLightCount || !crs.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds))
        {
            return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
        }

        shadowedOtherLights[shadowedOtherLightCount] = new ShadowedOtherLight
        {
            visibleLightIndex = visibleLightIndex,
            slopeScaleBias = light.shadowBias,
            normalBias = light.shadowNormalBias,
            isPoint = isPoint
        };
        Vector4 data = new Vector4(light.shadowStrength, shadowedOtherLightCount, isPoint ? 1f: 0f, maskChannel);
        shadowedOtherLightCount = newLightCount;
        return data;

    }

    public void Render(RenderGraphContext context)
    {
        _buffer = context.cmd;
        this.context = context.renderContext;
        
        if (shadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        // else
        // {
        //     _buffer.GetTemporaryRT(dirShadowAtlasId, 1,1,32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        // }
        
        if (shadowedOtherLightCount > 0)
        {
            RenderOtherShadows();
        }
        // else
        // {
        //     _buffer.SetGlobalTexture(otherShadowAtlasId, dirShadowAtlasId);
        // }
        
        _buffer.SetGlobalTexture(dirShadowAtlasId, directionalAtlas);
        _buffer.SetGlobalTexture(otherShadowAtlasId, otherAtlas);
        
        //是否使用阴影模板
        // cmb.BeginSample(cmbName);
        SetKeywords(shadowMaskKeywords, useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);
        //将级联数量和包围球数据发送到GPU
        _buffer.SetGlobalInt(cascadeCountId, shadowedDirectionalLightCount > 0 ? shadowSettings.directional.cascadeCount : 0);
        //阴影距离过渡相关数据发送GPU
        float f = 1f - shadowSettings.directional.cascadeFade;
        _buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(1f / shadowSettings.maxDistance, 1f / shadowSettings.distanceFade, 1f / (1f - f * f)));
        //传递图集大小和纹素大小
        _buffer.SetGlobalVector(shadowAtlasSizeId, atlasSizes);
        // cmb.EndSample(cmbName);
        ExecuteBuffer();
    }
    
    void RenderDirectionalShadows()
    {
        //创建rt, 并指定该类型是阴影贴图
        int atlasSize = (int)shadowSettings.directional.atlasSize;
        atlasSizes.x = atlasSize;
        atlasSizes.y = 1f / atlasSize;
        
        // _buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        _buffer.SetRenderTarget(directionalAtlas, RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        _buffer.ClearRenderTarget(true,false,Color.clear);
        _buffer.SetGlobalFloat(shadowPancakingId, 1f);
        _buffer.BeginSample("Directional Shadows");
        ExecuteBuffer();
        
        //要分割的图块大小和数量
        int tiles = shadowedDirectionalLightCount * shadowSettings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        
        for (int i = 0; i < shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        
        _buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
        //级联数据发送GPU
        _buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
        //阴影转换矩阵传入GPU
        _buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        //设置关键字
        SetKeywords(directionalFilterKeywords, (int)shadowSettings.directional.filter - 1);
        SetKeywords(cascadeBlendKeywords, (int)shadowSettings.directional.cascadeBlend - 1);
        _buffer.EndSample("Directional Shadows");
        ExecuteBuffer();
    }

    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(crs, light.visibleLightIndex, BatchCullingProjectionType.Orthographic)
        {
            useRenderingLayerMaskTest = true
        };
        //得到级联阴影贴图需要的参数
        int cascadeCount = shadowSettings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = shadowSettings.directional.CascadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - shadowSettings.directional.cascadeFade);
        float tileScale = 1f / split;
        for (int i = 0; i < cascadeCount; i++)
        {
            crs.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i, cascadeCount,ratios, tileSize, light.nearPlaneOffset,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData shadowSplitData);
            //剔除偏差
            shadowSplitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowDrawingSettings.splitData = shadowSplitData;
            //得到第一个光源的级联包围球数据
            if (index == 0)
            {
                //设置级联数据
                SetCascadeData(i, shadowSplitData.cullingSphere, tileSize);

            }
            //调整图块索引，它等于光源的图块偏移加上级联的索引
            int tileIndex = tileOffset + i;
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewPort(tileIndex, split, tileSize), tileScale);
            //设置视图投影矩阵
            _buffer.SetViewProjectionMatrices(viewMatrix,projectionMatrix);
            _buffer.SetGlobalDepthBias(0f,light.slopeScaleBias);
            //绘制阴影
            ExecuteBuffer();
            context.DrawShadows(ref shadowDrawingSettings);
            _buffer.SetGlobalDepthBias(0f,0f);
        }
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
        cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
    }

    void RenderOtherShadows()
    {
        //创建rt, 并指定该类型是阴影贴图
        int atlasSize = (int)shadowSettings.other.atlasSize;
        atlasSizes.z = atlasSize;
        atlasSizes.w = 1f / atlasSize;
        
        // _buffer.GetTemporaryRT(otherShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        _buffer.SetRenderTarget(otherAtlas, RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        _buffer.ClearRenderTarget(true,false,Color.clear);
        _buffer.SetGlobalFloat(shadowPancakingId, 0f);
        _buffer.BeginSample("Other Shadows");
        ExecuteBuffer();

        //要分割的图块大小和数量
        int tiles = shadowedOtherLightCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        
        for (int i = 0; i < shadowedOtherLightCount;)
        {
            if (shadowedOtherLights[i].isPoint)
            {
                RenderPointShadows(i, split, tileSize);
                i += 6;
            }
            else
            {
                RenderSpotShadows(i, split, tileSize);
                i += 1;
            }
        }
        //阴影转换矩阵传入GPU
        _buffer.SetGlobalMatrixArray(otherShadowMatricesId, otherShadowMatrices);
        _buffer.SetGlobalVectorArray(otherShadowTilesId, otherShadowTiles);
        SetKeywords(otherFilterKeywords, (int)shadowSettings.other.filter - 1);
        _buffer.EndSample("Other Shadows");
        ExecuteBuffer();
    }
    
    //渲染点光源阴影
    void RenderPointShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];
        ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(crs, light.visibleLightIndex, BatchCullingProjectionType.Perspective)
        {
            useRenderingLayerMaskTest = true
        };
        float texelSize = 2f / tileSize;
        float filterSize = texelSize * ((float)shadowSettings.other.filter + 1f);
        float bias = light.normalBias * filterSize * 1.4142136f;
        float tileScale = 1f / split;
        float fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;
        for (int i = 0; i < 6; i++)
        {
            crs.ComputePointShadowMatricesAndCullingPrimitives(light.visibleLightIndex, (CubemapFace)i,fovBias,out Matrix4x4 viewMatrix,out Matrix4x4 projectionMatrix,out ShadowSplitData shadowSplitData);
            viewMatrix.m11 = -viewMatrix.m11;
            viewMatrix.m12 = -viewMatrix.m12;
            viewMatrix.m13 = -viewMatrix.m13;
            
            shadowDrawingSettings.splitData = shadowSplitData;
            int tileIndex = index + i;
            Vector2 offset = SetTileViewPort(tileIndex, split, tileSize);
            SetOtherTileData(tileIndex, offset, tileScale, bias);
            otherShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);
            //设置视图投影矩阵
            _buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            //设置斜度比例偏差
            _buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            //绘制阴影
            ExecuteBuffer();
            context.DrawShadows(ref shadowDrawingSettings);
            _buffer.SetGlobalDepthBias(0f,0f);
        }

    }
    
    //渲染聚光灯阴影
    void RenderSpotShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];
        ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(crs, light.visibleLightIndex, BatchCullingProjectionType.Perspective)
        {
            useRenderingLayerMaskTest = true
        };
        crs.ComputeSpotShadowMatricesAndCullingPrimitives(light.visibleLightIndex, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData shadowSplitData);
        shadowDrawingSettings.splitData = shadowSplitData;
        float texelSize = 2f / (tileSize * projectionMatrix.m00);
        float filterSize = texelSize * ((float)shadowSettings.other.filter + 1f);
        float bias = light.normalBias * filterSize * 1.4142136f;
        Vector2 offset = SetTileViewPort(index, split, tileSize);
        float tileScale = 1f / split;
        SetOtherTileData(index, offset, tileScale, bias);
        otherShadowMatrices[index] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);
        //设置视图投影矩阵
        _buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        //设置斜度比例偏差
        _buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
        //绘制阴影
        ExecuteBuffer();
        context.DrawShadows(ref shadowDrawingSettings);
        _buffer.SetGlobalDepthBias(0f,0f);
    }
    
    //储存非定向光阴影图块数据
    void SetOtherTileData(int index, Vector2 offset, float scale, float bias)
    {
        float border = atlasSizes.w * 0.5f;
        Vector4 data;
        data.x = offset.x * scale + border;
        data.y = offset.y * scale + border;
        data.z = scale - border - border;
        data.w = bias;
        otherShadowTiles[index] = data;
    }

    //调整渲染视口来渲染单个图块
    Vector2 SetTileViewPort(int index, int split, float tileSize)
    {
        //计算图块索引的偏移量
        Vector2 offset = new Vector2(index % split, index / split);
        //设置渲染视口，拆分多个图块
        _buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }
    
    //返回一个从世界空间到阴影图块空间的转换矩阵
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale)
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
    void SetKeywords(GlobalKeyword[] keywords, int enableIndex)
    {
        // int enableIndex = (int)(shadowSettings.directional.filter - 1);
        for (int i = 0; i < keywords.Length; i++)
        {
            // if (i == enableIndex)
            // {
            //     _buffer.EnableShaderKeyword(keywords[i]);
            // }
            // else
            // {
            //     _buffer.DisableShaderKeyword(keywords[i]);
            // }
            _buffer.SetKeyword(keywords[i], i == enableIndex);
        }
    }

    // public void Cleanup()
    // {
    //     _buffer.ReleaseTemporaryRT(dirShadowAtlasId);
    //     if (shadowedOtherLightCount > 0)
    //     {
    //         _buffer.ReleaseTemporaryRT(otherShadowAtlasId);
    //     }
    //     ExecuteBuffer();
    // }
    
    public ShadowTextures GetRenderTextures(RenderGraph renderGraph, RenderGraphBuilder builder)
    {
        int atlasSize = (int)shadowSettings.directional.atlasSize;
        TextureDesc desc = new TextureDesc(atlasSize, atlasSize)
        {
            depthBufferBits = DepthBits.Depth32,
            isShadowMap = true,
            name = "Directional ShadowAtlas"
        };
        directionalAtlas = shadowedDirectionalLightCount > 0
            ? builder.WriteTexture(renderGraph.CreateTexture(desc))
            : renderGraph.defaultResources.defaultShadowTexture;
        atlasSize = (int)shadowSettings.other.atlasSize;
        desc.width = desc.height = atlasSize;
        desc.name = "Other Shadow Atlas";
        otherAtlas = shadowedOtherLightCount > 0
            ? builder.WriteTexture(renderGraph.CreateTexture(desc))
            : renderGraph.defaultResources.defaultShadowTexture;
        return new ShadowTextures(directionalAtlas, otherAtlas);
    }
}

