using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using System;
using System.Linq;

[CustomEditor(typeof(CarController), true)]
public class CarControllerEditor : OdinEditor
{
    CarController cC;
    float wireBoxScale = 0.1f;
    new void OnEnable() 
    {        
        cC = (CarController) target;
    }
    void OnSceneGUI() 
    {
        if(cC.showDebugHandles)
        {
            if(!cC.Wheels.Contains(null)) // wenn wheels gesetzt sind
            {            
                // starting Pos boxes
                Handles.DrawWireCube(cC.frontWheelR.StartingPos, Vector3.one * wireBoxScale);
                Handles.DrawWireCube(cC.frontWheelL.StartingPos, Vector3.one * wireBoxScale);
                Handles.DrawWireCube(cC.backWheelR.StartingPos, Vector3.one * wireBoxScale);
                Handles.DrawWireCube(cC.backWheelL.StartingPos, Vector3.one * wireBoxScale);

                // maxDistance lines
                Handles.DrawLine(cC.frontWheelR.StartingPos, cC.frontWheelR.StartingPos + (-cC.transform.up * cC.maximumLowRideDistance));
                Handles.DrawLine(cC.frontWheelL.StartingPos, cC.frontWheelL.StartingPos + (-cC.transform.up * cC.maximumLowRideDistance));
                Handles.DrawLine(cC.backWheelR.StartingPos, cC.backWheelR.StartingPos + (-cC.transform.up * cC.maximumLowRideDistance));
                Handles.DrawLine(cC.backWheelL.StartingPos, cC.backWheelL.StartingPos + (-cC.transform.up * cC.maximumLowRideDistance));

                //Show Rigidbody Things
                Rigidbody rB = cC.GetComponent<Rigidbody>();
                if(rB != null)
                {
                    Handles.color = Color.red;
                    Handles.DrawWireCube(rB.centerOfMass, Vector3.one * wireBoxScale/2f);
                    Handles.DrawLine(cC.transform.position + rB.centerOfMass,cC.transform.position +  rB.centerOfMass + cC.transform.forward * rB.velocity.magnitude * 0.25f, 5f);
                }
                
            }
            else
            {
                GUIStyle textStyle = new GUIStyle();
                textStyle.normal.textColor = Color.red;
                textStyle.fontSize = 30;
                textStyle.fontStyle = FontStyle.Bold;
                Handles.color = Color.red;

                Handles.DrawWireDisc(cC.transform.position,cC.transform.right,2f,5f);
                Handles.Label(cC.transform.position, cC.Wheels.Count(x => x == null).ToString(), textStyle);
            }

        }
    }
}
