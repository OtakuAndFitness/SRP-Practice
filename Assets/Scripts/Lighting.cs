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

    const int maximumLights = 4;

    static int mainLightColor = Shader.PropertyToID("_DirectionalLightColors");
    static int mainLightDir = Shader.PropertyToID("_DirectionalLightDirections");
    static int mainLightCounts = Shader.PropertyToID("_DirectionalLightCounts");

    static Vector4[] directionalColors = new Vector4[maximumLights];
    static Vector4[] directionalDirs = new Vector4[maximumLights];


    CullingResults crs;

    public void SetUp(ScriptableRenderContext context, CullingResults crs)
    {
        this.crs = crs;
        cmb.BeginSample(cmbName);
        // SetUpDirectionalLight();
        SetupLights();
        cmb.EndSample(cmbName);
        context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }

    void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = crs.visibleLights;

        int count = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight vl = visibleLights[i];
            if (vl.lightType == LightType.Directional)
            {
                //VisibleLight结构比较大，不要拷贝副本了
                SetUpDirectionalLight(count++, ref vl);
                if (count >= maximumLights)
                {
                    break;
                }
            }
        }
        cmb.SetGlobalInt(mainLightCounts,count);
        cmb.SetGlobalVectorArray(mainLightColor,directionalColors);
        cmb.SetGlobalVectorArray(mainLightDir,directionalDirs);

    }

    void SetUpDirectionalLight(int index, ref VisibleLight vl)
    {
        // Light light = RenderSettings.sun;
        
        directionalColors[index] = vl.finalColor;//需要去CustomeRenderPipelinen那里设置线性颜色才是线性的
        directionalDirs[index] = -vl.localToWorldMatrix.GetColumn(2);

    }
}
