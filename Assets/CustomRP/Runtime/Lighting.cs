using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{ 
    const string cmbName = "Lighting";

    CommandBuffer cmb = new CommandBuffer()
    {
        name = cmbName
    };

    const int maxDirLightCount = 4;
    //定义其他类型光源的最大数量
    const int maxOtherLightCount = 64;
    
    static string lightsPerObjectKeyword = "_LIGHTS_PER_OBJECT";

    private static int
        dirLightColorId = Shader.PropertyToID("_DirectionalLightColors"),
        dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"),
        dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
        dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData"),
        otherLightCountId = Shader.PropertyToID("_OtherLightCount"),
        otherLightColorsId = Shader.PropertyToID("_OtherLightColors"),
        otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions"),
        otherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections"),
        otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles"),
        otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");

    static Vector4[] 
        directionalColors = new Vector4[maxDirLightCount],
        directionalDirs = new Vector4[maxDirLightCount],
        dirLightShadowData = new Vector4[maxDirLightCount],
        otherLightColors = new Vector4[maxOtherLightCount],
        otherLightPositions = new Vector4[maxOtherLightCount],
        otherLightDirections = new Vector4[maxOtherLightCount],
        otherLightSpotAngles = new Vector4[maxOtherLightCount],
        otherLightShadowData = new Vector4[maxOtherLightCount];

    CullingResults crs;

    Shadows shadows = new Shadows();

    public void Setup(ScriptableRenderContext context, CullingResults crs, ShadowSettings shadowSettings, bool useLightsPerObject)
    {
        this.crs = crs;
        cmb.BeginSample(cmbName);
        //传递阴影数据
        shadows.Setup(context,crs,shadowSettings);
        //发送光源数据
        SetupLights(useLightsPerObject);
        shadows.Render();
        cmb.EndSample(cmbName);
        context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }

    void SetupLights(bool useLightsPerObject)
    {
        //拿到光源索引列表
        NativeArray<int> indexMap = useLightsPerObject ? crs.GetLightIndexMap(Allocator.Temp) : default;
        
        NativeArray<VisibleLight> visibleLights = crs.visibleLights;

        int dirLightCount = 0, otherLightCount = 0;
        int i;
        for (i = 0; i < visibleLights.Length; i++)
        {
            int newIndex = -1;
            VisibleLight vl = visibleLights[i];
            switch (vl.lightType)
            {
                case LightType.Directional:
                    if (dirLightCount < maxDirLightCount)
                    {
                        //VisibleLight结构比较大，不要拷贝副本了
                        SetupDirectionalLight(dirLightCount++, i, ref vl);
                    }
                    break;
                case LightType.Point:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        newIndex = otherLightCount;
                        SetupPointLight(otherLightCount++, i, ref vl);
                    }
                    break;
                case LightType.Spot:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        newIndex = otherLightCount;
                        SetupSpotLight(otherLightCount++, i, ref vl);
                    }
                    break;
            }
            if (useLightsPerObject)
            {
                indexMap[i] = newIndex;
            }
        }

        if (useLightsPerObject)
        {
            for (; i < indexMap.Length; i++)
            {
                indexMap[i] = -1;
            }
            crs.SetLightIndexMap(indexMap);
            indexMap.Dispose();
        }
        else
        {
            Shader.DisableKeyword(lightsPerObjectKeyword);
        }

        cmb.SetGlobalInt(dirLightCountId,dirLightCount);
        if (dirLightCount > 0)
        {
            cmb.SetGlobalVectorArray(dirLightColorId,directionalColors);
            cmb.SetGlobalVectorArray(dirLightDirectionsId,directionalDirs);
            cmb.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
        }
        
        cmb.SetGlobalInt(otherLightCountId, otherLightCount);
        if (otherLightCount > 0)
        {
            cmb.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
            cmb.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
            cmb.SetGlobalVectorArray(otherLightDirectionsId, otherLightDirections);
            cmb.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);
            cmb.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);
        }
        
    }

    //将聚光灯光源的颜色、位置和方向信息储存到数组
    void SetupSpotLight(int index, int visibleIndex, ref VisibleLight vl)
    {
        otherLightColors[index] = vl.finalColor;
        Vector4 position = vl.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(vl.range * vl.range, 0.00001f);
        otherLightPositions[index] = position;
        //本地到世界的转换矩阵的第三列再求反得到光照方向
        otherLightDirections[index] = -vl.localToWorldMatrix.GetColumn(2);

        Light light = vl.light;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * vl.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);

        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
    }

    void SetupPointLight(int index, int visibleIndex, ref VisibleLight vl)
    {
        otherLightColors[index] = vl.finalColor;
        //位置信息在本地到世界的转换矩阵的最后一列
        Vector4 position = vl.localToWorldMatrix.GetColumn(3);
        //将光照范围的平方的倒数储存在光源位置的w分量
        position.w = 1f / Mathf.Max(vl.range * vl.range, 0.00001f);
        otherLightPositions[index] = position;

        otherLightSpotAngles[index] = new Vector4(0f, 1f);
        
        Light light = vl.light;
        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
    }

    void SetupDirectionalLight(int index, int visibleIndex, ref VisibleLight vl)
    {
        directionalColors[index] = vl.finalColor;//需要去CustomRenderPipeline那里设置线性颜色才是线性的
        directionalDirs[index] = -vl.localToWorldMatrix.GetColumn(2);
        
        //储存阴影数据
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(vl.light, visibleIndex);
    }

    public void Cleanup()
    {
        shadows.Cleanup();
    }
}
