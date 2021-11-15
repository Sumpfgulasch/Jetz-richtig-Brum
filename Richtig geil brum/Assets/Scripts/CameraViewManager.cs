using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Cinemachine;
public class CameraViewManager : SerializedMonoBehaviour
{
    Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;    
    }

    [Button]
    public void SelectCamera(CameraTypes _cameraType)
    {
        switch (_cameraType)
        {
            case CameraTypes.SideCameraNoRotation:
                break;
            case CameraTypes.FollowCamera:
                break;
        }

    }

    public enum CameraTypes
{ 
    SideCameraNoRotation,
    FollowCamera
}
}