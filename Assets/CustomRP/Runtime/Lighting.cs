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

    static int 
        dirLightColorId = Shader.PropertyToID("_DirectionalLightColors"),
        dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"),
        dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
        dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    static Vector4[] 
        directionalColors = new Vector4[maxDirLightCount],
        directionalDirs = new Vector4[maxDirLightCount],
        dirLightShadowData = new Vector4[maxDirLightCount];

    CullingResults crs;

    Shadows shadows = new Shadows();

    public void SetUp(ScriptableRenderContext context, CullingResults crs, ShadowSettings shadowSettings)
    {
        this.crs = crs;
        cmb.BeginSample(cmbName);
        //传递阴影数据
        shadows.Setup(context,crs,shadowSettings);
        //发送光源数据
        SetupLights();
        shadows.Render();
        cmb.EndSample(cmbName);
        context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }

    void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = crs.visibleLights;

        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight vl = visibleLights[i];
            if (vl.lightType == LightType.Directional)
            {
                //VisibleLight结构比较大，不要拷贝副本了
                SetupDirectionalLight(dirLightCount++, ref vl);
                if (dirLightCount >= maxDirLightCount)
                {
                    break;
                }
            }
        }
        cmb.SetGlobalInt(dirLightCountId,dirLightCount);
        cmb.SetGlobalVectorArray(dirLightColorId,directionalColors);
        cmb.SetGlobalVectorArray(dirLightDirectionsId,directionalDirs);
        
        cmb.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);

    }

    void SetupDirectionalLight(int index, ref VisibleLight vl)
    {
        // Light light = RenderSettings.sun;
        
        directionalColors[index] = vl.finalColor;//需要去CustomRenderPipeline那里设置线性颜色才是线性的
        directionalDirs[index] = -vl.localToWorldMatrix.GetColumn(2);
        
        //储存阴影数据
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(vl.light, index);
    }

    public void CleanUp()
    {
        shadows.CleanUp();
    }
}
