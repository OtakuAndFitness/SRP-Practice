using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

partial class CameraRenderer
{
    partial void DrawUnsupportShaders();
    partial void DrawGizmos();
    partial void PrepareForSceneWindow();
    partial void PrepareBuffer();

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

    private string SampleName { get; set; }

    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        cmb.name = SampleName = camera.name;
        Profiler.EndSample();
    }

    partial void PrepareForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            //切到Scene窗口绘制东西
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera,GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera,GizmoSubset.PostImageEffects);
        }
    }

    partial void DrawUnsupportShaders()
    {
        if (errorMat == null)
        {
            errorMat = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        DrawingSettings dss = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera))
        {
            overrideMaterial = errorMat
        };
        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            dss.SetShaderPassName(i,legacyShaderTagIds[i]);
        }

        FilteringSettings fss = FilteringSettings.defaultValue;
        context.DrawRenderers(crs,ref dss, ref fss);
    }
#else
    const string SampleName = cmbName;
#endif
    
}
