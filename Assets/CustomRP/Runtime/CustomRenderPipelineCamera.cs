using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public class CustomRenderPipelineCamera : MonoBehaviour
{
    [SerializeField] 
    CameraSettings cameraSettings = default;

    public CameraSettings CameraSettings => cameraSettings ?? (cameraSettings = new CameraSettings());
}
