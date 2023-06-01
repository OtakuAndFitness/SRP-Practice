using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light), typeof(CustomRenderPipelineAsset))]
public class CustomLightEditor : LightEditor
{
    //Spot Light Inner/Outer Angle显示在面板上, 2021.3需要，2020.3直接有显示，不需要
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        // DrawRenderingLayerMask();
        RenderingLayerMaskDrawer.Draw(settings.renderingLayerMask, renderLayerMaskLabel);
        
        if (!settings.lightType.hasMultipleDifferentValues &&
            (LightType)settings.lightType.enumValueIndex == LightType.Spot)
        {
            settings.DrawInnerAndOuterSpotAngle();
        }
        
        settings.ApplyModifiedProperties();

        Light light = target as Light;
        if (light.cullingMask != -1)
        {
            EditorGUILayout.HelpBox(light.type == LightType.Directional ? "Culling Mask only affects shadows." : "Culling Mask only affects shadow unless Use Lights Per Objects is on", MessageType.Warning);
        }
    }

    static GUIContent renderLayerMaskLabel =
        new GUIContent("Rendering Layer Mask", "Functional version of above property.");

    // void DrawRenderingLayerMask()
    // {
    //     SerializedProperty property = settings.renderingLayerMask;
    //     EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
    //     EditorGUI.BeginChangeCheck();
    //     int mask = property.intValue;
    //     if (mask == int.MaxValue)
    //     {
    //         mask = -1;
    //     }
    //     mask = EditorGUILayout.MaskField(renderLayerMaskLabel, mask,
    //         GraphicsSettings.currentRenderPipeline.renderingLayerMaskNames);
    //     if (EditorGUI.EndChangeCheck())
    //     {
    //         property.intValue = mask == -1 ? int.MaxValue : mask;
    //     }
    //
    //     EditorGUI.showMixedValue = false;
    // }
}
