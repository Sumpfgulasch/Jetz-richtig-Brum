using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Rigidbody))]
public class TeleportBehavior : CarBehavior
{
    const string R = "References";
    const string S = "Settings";
    const string I = "Input";

    [TitleGroup(R)] private Rigidbody rB;
    [TitleGroup(I)] public bool teleportOnNextFrame = false;

    //------------------------ SETUP
    public override bool SetRequirements()
    {
        // SET THE RIGIDBODY.
        rB = cC.gameObject.GetComponent<Rigidbody>();
        if (rB == null)
        {
            rB = cC.gameObject.AddComponent<Rigidbody>();
        }

        //CHECK IF INITIALISATION WAS SUCCESSFULL
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
    public void OnTeleport(InputValue inputValue)
    {
        if (EnabledBehavior)
        {
            teleportOnNextFrame = true;
        }
    }

    //------------------------ BEHAVIOR
    public override void ExecuteBehavior(Func<bool> _shouldExecute)
    {
        if (teleportOnNextFrame)
        {
            if (_shouldExecute())
            {
                // 1. Put up
                transform.position += new Vector3(0, 5, 0);
                // 2. rotate up
                rB.MoveRotation(Quaternion.LookRotation(transform.forward, Vector3.up));
                rB.angularVelocity = Vector3.zero;

                // 3. ResetTeleportBool
                teleportOnNextFrame = false;
            }
        }
    }




}
