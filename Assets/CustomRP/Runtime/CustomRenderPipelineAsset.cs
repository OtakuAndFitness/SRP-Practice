using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//右键->Create菜单中t添加一个新的子菜单
[CreateAssetMenu(menuName="Rendering/CreateCustomRenderPipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField] 
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true, useLightsPerObject = true, allowHDR = true;
    
    [SerializeField]
    PostFXSettings postFXSettings = default;
    
    [SerializeField] 
    ShadowSettings shadowSettings = default;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(allowHDR,useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadowSettings, postFXSettings);
    }
}
