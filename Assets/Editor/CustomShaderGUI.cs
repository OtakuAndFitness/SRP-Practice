using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{ 
    MaterialEditor editor;
    Object[] materials;
    MaterialProperty[] properties;

    bool showPreset;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);

        editor = materialEditor;
        materials = materialEditor.targets;
        this.properties = properties;
        
        EditorGUILayout.Space();
        showPreset = EditorGUILayout.Foldout(showPreset, "Preset", true);
        if (showPreset)
        {
            OpaquePreset();
            ClipPreset();
            TransparentPreset();
            PreAlpha();
        }
    }

    bool HasProperty(string name) => FindProperty(name, properties, false) != null;
    private bool HasPremultipyAlpha => HasProperty("_PremultipyAlpha");

    bool SetProperty(string name, float value)
    {
        MaterialProperty property = FindProperty(name, properties, false);
        if (property != null)
        {
            property.floatValue = value;
            return true;
        }

        return false;
    }

    void SetProperty(string name, string keyword, bool value)
    {
        if (SetProperty(name, value ? 1f : 0f))
        {
            SetKeyword(keyword, value);
        }
    }

    void SetKeyword(string keyword, bool enabled)
    {
        if (enabled)
        {
            foreach (Material mat in materials)
            {
                mat.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach (Material mat in materials)
            {
                mat.DisableKeyword(keyword);
            }
        }
    }

    bool Clipping
    {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }
    
    bool PremultipyAlpha
    {
        set => SetProperty("_PremultipyAlpha", "_PREMULTIPY_ALPHA", value);
    }

    BlendMode SrcBlend
    {
        set => SetProperty("_SrcBlend", (float) value);
    }
    
    BlendMode DstBlend
    {
        set => SetProperty("_DstBlend", (float) value);
    }
    
    bool ZWrite
    {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }

    RenderQueue RenderQueue
    {
        set
        {
            foreach (Material mat in materials)
            {
                mat.renderQueue = (int) value;
            }
        }
    }

    bool PresetButton(string name)
    {
        if (GUILayout.Button(name))
        {
            editor.RegisterPropertyChangeUndo(name);
            return true;
        }

        return false;
    }

    void OpaquePreset()
    {
        if (PresetButton("Opaque"))
        {
            Clipping = false;
            PremultipyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;
        }
    }

    void ClipPreset()
    {
        if (PresetButton("Clip"))
        {
            Clipping = true;
            PremultipyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest;
        }
    }

    void TransparentPreset()
    {
        if (PresetButton("Transparent"))
        {
            Clipping = false;
            PremultipyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
    
    void PreAlpha()
    {
        if (HasPremultipyAlpha && PresetButton("PremultipyAlpha"))
        {
            Clipping = false;
            PremultipyAlpha = true;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
    
}
