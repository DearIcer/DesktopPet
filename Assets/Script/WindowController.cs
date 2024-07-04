using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowController : MonoBehaviour
{
    public enum TransparentType : int
    {
        None = 0,
        Alpha = 1,
        ColorKey = 2,
    }
    [Tooltip("Main camera is used if None")]
    public Camera currentCamera;
    [Header("Advanced settings")]
    [Tooltip("Change camera background when the window is transparent")]
    public bool autoSwitchCameraBackground = true;
    [Header("For Windows only")]
    [Tooltip("Select the method. *Only available on Windows")]
    public TransparentType transparentType = TransparentType.Alpha;
    [Tooltip("Will be used the next time the window becomes transparent")]
    public Color32 keyColor = new Color32(0x01, 0x00, 0x01, 0x00);
    private CameraClearFlags _originalCameraClearFlags;
    private Color _originalCameraBackground;

    
    void SetCameraBackground(bool transparent)
    {
        // todo:首先检查currentCamera是否存在以及是否允许自动切换相机背景。如果这两个条件中的任何一个不满足，则方法直接返回，不做任何操作
        if (!currentCamera || !autoSwitchCameraBackground) return;

        // todo: 处理透明
        if (transparent)
        {
            currentCamera.clearFlags = CameraClearFlags.SolidColor;
            if (transparentType == TransparentType.ColorKey)
            {
                currentCamera.backgroundColor = keyColor;
            }
            else
            {
                currentCamera.backgroundColor = Color.clear;
            }
        }
        else
        {
            currentCamera.clearFlags = _originalCameraClearFlags;
            currentCamera.backgroundColor = _originalCameraBackground;
        }
    }
}
