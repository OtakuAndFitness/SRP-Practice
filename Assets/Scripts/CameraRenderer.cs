using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    ScriptableRenderContext context;
    Camera camera;
    const string cmbName = "Custom Command Buffer";
    CommandBuffer cmb = new CommandBuffer()
    {
        name = cmbName
    };
    //摄像机剔除结果
    CullingResults crs;
    static ShaderTagId unlitId = new ShaderTagId("SRPDefaultUnlit");
    
    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing)
    {
        this.context = context;
        this.camera = camera;
        
        PrepareBuffer();
        
        PrepareForSceneWindow();

        if (!Cull())
        {
            return;
        }

        Setup();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        
        //暴露srp不支持的shader
        DrawUnsupportShaders();
        
        //绘制Gizmos
        DrawGizmos();

        //context发送的渲染命令都是缓冲的，所以要通过submit来提交命令
        Submit();
    }

    bool Cull()
    {
        ScriptableCullingParameters parameters;
        if (camera.TryGetCullingParameters(out parameters))
        {
            crs = context.Cull(ref parameters);
            return true;
        }

        return false;
    }

    //设置相机的属性和矩阵
    void Setup()
    {
        context.SetupCameraProperties(camera);
        //得到相机清除状态
        CameraClearFlags ccfs = camera.clearFlags;
        //设置相机清除状态
        cmb.ClearRenderTarget(ccfs<=CameraClearFlags.Depth,ccfs == CameraClearFlags.Color,ccfs == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        cmb.BeginSample(SampleName);
        ExecuteBuffer();//为了采样
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        SortingSettings sss = new SortingSettings()
        {
            criteria = SortingCriteria.CommonOpaque
        };
        //设置渲染的pass和排序模式
        DrawingSettings dss = new DrawingSettings(unlitId,sss)
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing
        };
        //哪些类型的渲染队列会被渲染
        FilteringSettings fss = new FilteringSettings(RenderQueueRange.opaque);
        //先渲染不透明物体
        context.DrawRenderers(crs,ref dss,ref fss);
        
        //再渲染天空盒
        context.DrawSkybox(camera);
        
        //最后渲染透明物体
        sss.criteria = SortingCriteria.CommonTransparent;
        dss.sortingSettings = sss;
        fss.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(crs,ref dss,ref fss);


    }

    void Submit()
    {
        cmb.EndSample(SampleName);
        ExecuteBuffer();//真正执行
        context.Submit();
    } 
}
