using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public class CustomRenderPipelineCamera : MonoBehaviour
{
    [SerializeField] 
    CameraSettings cameraSettings = default;

    private ProfilingSampler _sampler;

    public ProfilingSampler Sampler => _sampler ??= new(GetComponent<Camera>().name);
    // public CameraSettings CameraSettings => cameraSettings ?? (cameraSettings = new CameraSettings());
    public CameraSettings Settings => cameraSettings ??= new();
    
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnEnable() => _sampler = null;
#endif
}
