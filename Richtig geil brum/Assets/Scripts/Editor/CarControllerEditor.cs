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
            if(!cC.Wheels.Contains(null)) // wenn wheels gesetzt sind - bzw kein wheel null ist
            {            
                //starting Pos boxes
                Handles.DrawWireCube(cC.frontWheelR.transform.position, Vector3.one * wireBoxScale);
                Handles.DrawWireCube(cC.frontWheelL.transform.position, Vector3.one * wireBoxScale);
                Handles.DrawWireCube(cC.backWheelR.transform.position, Vector3.one * wireBoxScale);
                Handles.DrawWireCube(cC.backWheelL.transform.position, Vector3.one * wireBoxScale);

                //maxDistance lines
                Handles.DrawLine(cC.frontWheelR.transform.position, cC.frontWheelR.transform.position + (-cC.transform.up * cC.frontWheelR.wheelCollider.suspensionDistance));
                Handles.DrawLine(cC.frontWheelL.transform.position, cC.frontWheelL.transform.position + (-cC.transform.up * cC.frontWheelL.wheelCollider.suspensionDistance));
                Handles.DrawLine(cC.backWheelR.transform.position, cC.backWheelR.transform.position + (-cC.transform.up * cC.backWheelR.wheelCollider.suspensionDistance));
                Handles.DrawLine(cC.backWheelL.transform.position, cC.backWheelL.transform.position + (-cC.transform.up * cC.backWheelL.wheelCollider.suspensionDistance));

                //Show Rigidbody Things
                Rigidbody rB = cC.GetComponent<Rigidbody>();
                if(rB != null)
                {
                    Vector3 com = cC.transform.rotation * rB.centerOfMass;
                    Handles.color = Color.red;
                    Handles.DrawWireCube(com + cC.transform.position, Vector3.one * wireBoxScale*3f);
                    Handles.DrawLine(cC.transform.position + com,cC.transform.position +  rB.centerOfMass + cC.transform.forward * rB.velocity.magnitude * 0.25f, 5f);
                    Handles.Label(cC.transform.position + com + Vector3.down * 0.14f,rB.velocity.magnitude.ToString());
                }

                //Show raycast Hits
                Handles.color = Color.red;

                RaycastHit hit;
                if(Physics.Raycast(cC.transform.position, cC.transform.rotation * Vector3.down, out hit))
                {
                    if(cC.drivingStateInfo == DrivingState.InAir) // inAir
                    {
                        float distanceToGround = (hit.point - cC.transform.position).magnitude;
                        if(distanceToGround < cC.autoalignSurfaceDistance)
                        {
                            Handles.color = Color.green;
                            Handles.DrawLine(hit.point, hit.point + hit.normal, 1f);//would align
                        }
                        else
                        {
                            Handles.DrawLine(hit.point, hit.point + hit.normal, 1f);//would not align
                        }
                    }
                    else // grounded
                    {
                       Handles.DrawLine(hit.point, hit.point + hit.normal, 1f);//would not align
                    }
                }
                else
                {
                    Debug.Log("RC didnt hit");
                }

                //Show AirRollCenterOffset
                Handles.color = Color.magenta;
                Handles.Label(cC.transform.position + cC.airRollCenterOffset,"rotationCenter");
                Handles.DrawWireCube(cC.transform.position + (cC.transform.rotation * cC.airRollCenterOffset), Vector3.one * 1.4f * wireBoxScale);
                
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
