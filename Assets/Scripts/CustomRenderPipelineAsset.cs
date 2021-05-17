using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//右键->Create菜单中t添加一个新的子菜单
[CreateAssetMenu(menuName="Rendering/CreateCustomRenderPipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline();
    }
}
