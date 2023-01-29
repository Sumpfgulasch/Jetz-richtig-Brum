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
    MagnetBehavior mB;
    ConnectWheelsToGroundBehavior cWG;
    float wireBoxScale = 0.1f;



    new void OnEnable()
    {
        cC = (CarController) target;

        if (cC != null)
        {
            if(cC.HasBehavior<MagnetBehavior>())
            {
                mB = cC.GetBehavior<MagnetBehavior>();
            }
            if (cC.HasBehavior<ConnectWheelsToGroundBehavior>())
            {
                cWG = cC.GetBehavior<ConnectWheelsToGroundBehavior>();
            }
        }

        SceneView.duringSceneGui += CustomOnSceneGUI;
    }

    new void OnDisable()
    {
        SceneView.duringSceneGui -= CustomOnSceneGUI;
    }

    private void CustomOnSceneGUI(SceneView view)
    {
        if(cC.showDebugHandles)
        {
            GUIStyle textStyle = new GUIStyle();
            textStyle.fontStyle = FontStyle.Bold;


            if (!cC.Wheels.Contains(null)) // wenn wheels gesetzt sind - bzw kein wheel null ist
            {
                //starting Pos boxes
                Handles.DrawWireCube(cC.frontWheelR.transform.position, Vector3.one * wireBoxScale);
                Handles.DrawWireCube(cC.frontWheelL.transform.position, Vector3.one * wireBoxScale);
                Handles.DrawWireCube(cC.backWheelR.transform.position, Vector3.one * wireBoxScale);
                Handles.DrawWireCube(cC.backWheelL.transform.position, Vector3.one * wireBoxScale);

                ////maxDistance lines
                //Handles.DrawLine(cC.frontWheelR.transform.position, cC.frontWheelR.transform.position + (-cC.transform.up * cC.frontWheelR.wheelCollider.suspensionDistance));
                //Handles.DrawLine(cC.frontWheelL.transform.position, cC.frontWheelL.transform.position + (-cC.transform.up * cC.frontWheelL.wheelCollider.suspensionDistance));
                //Handles.DrawLine(cC.backWheelR.transform.position, cC.backWheelR.transform.position + (-cC.transform.up * cC.backWheelR.wheelCollider.suspensionDistance));
                //Handles.DrawLine(cC.backWheelL.transform.position, cC.backWheelL.transform.position + (-cC.transform.up * cC.backWheelL.wheelCollider.suspensionDistance));

                //Show Rigidbody Things
                Rigidbody rB = cC.GetComponent<Rigidbody>();
                if(rB != null)
                {
                    Vector3 com = cC.transform.rotation * rB.centerOfMass;

                    Handles.color = Color.green;
                    textStyle.fontSize = 15;
                    textStyle.normal.textColor = Color.green;
                    Handles.DrawWireCube(com + cC.transform.position, Vector3.one * wireBoxScale*1.5f); // center of mass box
                    Handles.Label(com + cC.transform.position + Vector3.up * 0.14f, "CenterOfMass", textStyle); // Center of Mass label

                    Handles.color = Color.red;
                    textStyle.fontSize = 15;
                    textStyle.normal.textColor = Color.red;
                    //Handles.DrawLine(cC.transform.position + com , cC.transform.position +  rB.centerOfMass + cC.transform.forward * rB.velocity.magnitude * 0.25f, 5f); // velocity line
                    Handles.Label(cC.transform.position + com + Vector3.down * 0.14f, "velocity: " +  rB.velocity.magnitude.CutNumberAtDecimalPlace(2).ToString() + " units Per Sec",textStyle); // velocity as a number
                }


                //Show AirRollCenterOffset
                //Handles.color = Color.magenta;
                //Handles.Label(cC.transform.position + cC.airRollCenterOffset,"rotationCenter");
                //Handles.DrawWireCube(cC.transform.position + (cC.transform.rotation * cC.airRollCenterOffset), Vector3.one * 1.4f * wireBoxScale);

            }
            else
            {
                textStyle.normal.textColor = Color.red;
                textStyle.fontSize = 30;
                Handles.color = Color.red;
                Handles.DrawWireDisc(cC.transform.position,cC.transform.right,2f,5f);
                Handles.Label(cC.transform.position, cC.Wheels.Count(x => x == null).ToString(), textStyle);
            }

            if (mB != null)
            {
                for (int i = 0; i < mB.magnetForcePositions.Length; i++)
                {
                    Handles.color = Color.magenta;
                    Handles.DrawWireCube(mB.magnetForcePositions[i].position, Vector3.one * 0.3f);
                    Handles.color = Color.red;
                    Handles.DrawWireCube(mB.magnetForcePositions[i].position, Vector3.one * 0.01f);
                }
                Handles.Label(mB.magnetForcePositions[0].position, "Front");
                Handles.Label(mB.magnetForcePositions[1].position, "Back");
            }
            //if (cWG != null)
            //{
            //    Handles.color = Color.red;
            //    float thickness = 8f;
            //    //frontWheelL
            //    Handles.Label(cC.frontWheelLRest.transform.position, cWG.frontWheelLDistance.ToString());
            //    Handles.DrawLine(cC.frontWheelLRest.transform.position, cC.frontWheelLRest.transform.position + (cC.frontWheelLRest.transform.up * cWG.frontWheelLDistance), thickness);
            //    //frontWheelR
            //    Handles.Label(cC.frontWheelRRest.transform.position, cWG.frontWheelRDistance.ToString());
            //    Handles.DrawLine(cC.frontWheelRRest.transform.position, cC.frontWheelRRest.transform.position + (cC.frontWheelRRest.transform.up * cWG.frontWheelRDistance), thickness);
            //    //backWheelL
            //    Handles.Label(cC.backWheelLRest.transform.position, cWG.backWheelLDistance.ToString());
            //    Handles.DrawLine(cC.backWheelLRest.transform.position, cC.backWheelLRest.transform.position + (cC.backWheelLRest.transform.up * cWG.backWheelLDistance), thickness);
            //    //backWheelR
            //    Handles.Label(cC.backWheelRRest.transform.position, cWG.backWheelRDistance.ToString());
            //    Handles.DrawLine(cC.backWheelRRest.transform.position, cC.backWheelRRest.transform.position + (cC.backWheelRRest.transform.up * cWG.backWheelRDistance), thickness);
            //}
        }
    }
}

public class CarControllerGizmos
{
    [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.NotInSelectionHierarchy)]
    static void DrawGizmoForMyScript(CarController scr, GizmoType gizmoType)
    {
        Vector3 position = scr.transform.position;

        //if (Vector3.Distance(position, Camera.current.transform.position) > 10f)
        Gizmos.DrawIcon(position, "stopwatch.png");
        //Gizmos.DrawLine
    }
}