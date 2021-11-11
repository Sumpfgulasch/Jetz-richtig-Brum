using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Rigidbody))]
public class JumpBehavior : CarBehavior
{
    const string R = "References";
    const string S = "Settings";
    const string I = "Input";

    [TitleGroup(S)] public float jumpForce = 10.3f;
    [TitleGroup(I)] private bool jumpNextFrame = false;

    [TitleGroup(R)] private Rigidbody rB = null;

    [TitleGroup(I)] private bool hasMagnetBehavior = false;
    [TitleGroup(R)] private MagnetBehavior magnetBehavior = null;

    //------------------------ SETUP
    public override bool SetRequirements()
    {
        // GET MAGNET BEHAVIOR
        if (cC.HasBehavior<MagnetBehavior>())
        {
            magnetBehavior = cC.GetBehavior<MagnetBehavior>();
            hasMagnetBehavior = true;
        }

        // SET THE RIGIDBODY.
        rB = cC.gameObject.GetComponent<Rigidbody>();
        if (rB == null)
        {
            rB = cC.gameObject.AddComponent<Rigidbody>();
        }

        //CHECK IF INITIALISATION WAS SUCCESSFULL

        if (hasMagnetBehavior == true && magnetBehavior == null){ return false; }

        if (rB == null) // double check if it is still null, but that should never be the case.
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    //------------------------ INPUT HANDLING

    public void OnJump(InputValue inputValue)
    {
        if (cC.drivingStateInfo == DrivingState.Grounded && EnabledBehavior)
        {
            jumpNextFrame = true;
        }
    }

    //------------------------ BEHAVIOR
    public override void ExecuteBehavior(Func<bool> _shouldExecute)
    {
        if (jumpNextFrame)
        { 
            // 1. Deactivate magnet, if cC has magnet behavior
            if (hasMagnetBehavior)
            { 
                magnetBehavior.MagnetIsActive = false;
                magnetBehavior.ToggleExtendedGroundDistance(false);
            }

            // 2. add up-force
            rB.AddForce(transform.up * jumpForce, ForceMode.VelocityChange);

            // 3. Reset Jumpbool
            jumpNextFrame = false;

        }
    }


}
