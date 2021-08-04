using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using System;

[CustomEditor(typeof(CarController), true)]
public class CarControllerEditor : OdinEditor
{
    CarController carController;
    float wireBoxScale = 0.1f;
    new void OnEnable() 
    {        
        carController = (CarController) target;
    }
    void OnSceneGUI() 
    {
        if(carController.showDebugHandles)
        {
            // starting Pos boxes
            Handles.DrawWireCube(carController.StartingPosBackWheelL, Vector3.one * wireBoxScale);
            Handles.DrawWireCube(carController.StartingPosBackWheelR, Vector3.one * wireBoxScale);
            Handles.DrawWireCube(carController.StartingPosFrontWheelL, Vector3.one * wireBoxScale);
            Handles.DrawWireCube(carController.StartingPosFrontWheelR, Vector3.one * wireBoxScale);

            // maxDistance lines
            Handles.DrawLine(carController.StartingPosBackWheelL, carController.StartingPosBackWheelL + (-carController.transform.up * carController.maximumLowRideDistance));
            Handles.DrawLine(carController.StartingPosBackWheelR, carController.StartingPosBackWheelR + (-carController.transform.up * carController.maximumLowRideDistance));
            Handles.DrawLine(carController.StartingPosFrontWheelL, carController.StartingPosFrontWheelL + (-carController.transform.up * carController.maximumLowRideDistance));
            Handles.DrawLine(carController.StartingPosFrontWheelR, carController.StartingPosFrontWheelR + (-carController.transform.up * carController.maximumLowRideDistance));
        }
    }
}
