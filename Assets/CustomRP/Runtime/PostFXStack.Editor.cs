using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

partial class PostFXStack
{
    partial void ApplySceneViewState();
    
#if UNITY_EDITOR
    partial void ApplySceneViewState()
    {
        if (camera.cameraType == CameraType.SceneView &&
            !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
        {
            postFXSettings = null;
        }
    }
#endif
}