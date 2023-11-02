using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

partial class CameraRenderer
{
    // partial void DrawUnsupportedShaders();
    // partial void DrawGizmosBeforeFX();
    // partial void DrawGizmosAfterFX();
    partial void PrepareForSceneWindow();
    // partial void PrepareBuffer();

#if UNITY_EDITOR
    private static ShaderTagId[] legacyShaderTagIds =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM"),

    };

    static Material errorMat;

    // private string SampleName { get; set; }

    // partial void PrepareBuffer()
    // {
    //     Profiler.BeginSample("Editor Only");
    //     cmb.name = SampleName = camera.name;
    //     Profiler.EndSample();
    // }

    partial void PrepareForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            //切到Scene窗口绘制东西
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            useScaledRendering = false;
        }
    }

    // partial void DrawGizmosBeforeFX()
    // {
    //     if (Handles.ShouldRenderGizmos())
    //     {
    //         if (useIntermediateBuffer)
    //         {
    //             Draw(depthAttachmentId, BuiltinRenderTextureType.CameraTarget, true);
    //             ExecuteBuffer();
    //         }
    //         context.DrawGizmos(camera,GizmoSubset.PreImageEffects);
    //     }
    // }
    
    // partial void DrawGizmosAfterFX()
    // {
    //     if (Handles.ShouldRenderGizmos())
    //     {
    //         if (postFXStack.IsActive)
    //         {
    //             Draw(depthAttachmentId, BuiltinRenderTextureType.CameraTarget, true);
    //             ExecuteBuffer();
    //         }
    //         context.DrawGizmos(camera,GizmoSubset.PostImageEffects);
    //     }
    // }

    /// <summary>
    /// 绘制SRP不支持的着色器类型
    /// </summary>
    public void DrawUnsupportedShaders()
    {
        if (errorMat == null)
        {
            errorMat = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        //数组第一个元素用来构造DrawingSettings对象的时候设置
        DrawingSettings dss = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera))
        {
            overrideMaterial = errorMat
        };
        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            //遍历数组逐个设置着色器的PasssName, 从i=1开始
            dss.SetShaderPassName(i,legacyShaderTagIds[i]);
        }
        //使用默认设置即可，反正画出来的都是不支持的
        FilteringSettings fss = FilteringSettings.defaultValue;
        //绘制不支持的ShaderTag类型的物体
        context.DrawRenderers(crs,ref dss, ref fss);
    }
// #else
//     const string SampleName = cmbName;
#endif
    
}
