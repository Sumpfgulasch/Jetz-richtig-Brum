using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using System.Linq;


public class ConnectWheelsToGroundBehavior : CarBehavior
{
    const string R = "References";
    const string S = "Settings";
    const string I = "Input";
    const string H = "Helper";
    const string D = "Debug";



    [TitleGroup(S)] [Range(0, 1f)] public float maxConnectionDistance = 0.5f;



    [TitleGroup(R)] private Wheel frontWheelR, frontWheelL, backWheelR, backWheelL;
    [TitleGroup(R)] private Wheel[] Wheels { get { return new Wheel[4] { frontWheelR, frontWheelL, backWheelR, backWheelL }; } }
    [TitleGroup(R)] [ShowInInspector] public float frontWheelLDistance, frontWheelRDistance, backWheelLDistance, backWheelRDistance;



    //------------------------ SETUP
    public override bool SetRequirements()
    {
        //SETUP THE WHEELS
        frontWheelR = cC.frontWheelR != null ? cC.frontWheelR : null;
        frontWheelL = cC.frontWheelL != null ? cC.frontWheelL : null;
        backWheelR = cC.backWheelR != null ? cC.backWheelR : null;
        backWheelL = cC.backWheelL != null ? cC.backWheelL : null;

        //CHECK IF INITIALISATION WAS SUCCESSFULL
        if (Wheels.Contains(null))
        {
            Debug.LogWarning(this.transform.name + ": ConnectWheelsToGround Behavior cant be executed Properly.");
            return false;
        }
        else
        {
            return true;
        }
    }

    //------------------------ BEHAVIOR
    public override void ExecuteBehavior(Func<bool> _shouldExecute)
    {
        // Get the WheelCollider position to the ground
        RaycastHit hit;
        if (Physics.Raycast(frontWheelL.transform.position, frontWheelL.transform.up, out hit))
        {
            frontWheelLDistance = (hit.point - cC.frontWheelLRest.transform.position).magnitude;
        }
        else
        {
            frontWheelLDistance = 0f;
        }
        if (Physics.Raycast(frontWheelR.transform.position, frontWheelR.transform.up, out hit))
        {
            frontWheelRDistance = (hit.point - cC.frontWheelRRest.transform.position).magnitude;
        }
        else
        {
            frontWheelRDistance = 0f;
        }
        if (Physics.Raycast(backWheelL.transform.position, backWheelL.transform.up, out hit))
        {
            backWheelLDistance = (hit.point - cC.backWheelLRest.transform.position).magnitude;
        }
        else
        {
            backWheelLDistance = 0f;
        }
        if (Physics.Raycast(backWheelR.transform.position, backWheelR.transform.up, out hit))
        {
            backWheelRDistance = (hit.point - cC.backWheelR.transform.position).magnitude;
        }
        else
        {
            backWheelRDistance = 0f;
        }
    }

}
