using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Rigidbody))]
public class AutoBreakInAirBehavior : CarBehavior
{
    //TODO - Autoalign behavior has Break functionality, this could be merged into this autobreak feature.

    const string R = "References";
    const string S = "Settings";
    const string I = "Input";


    [TitleGroup(S)] [Range(0, 1f)] public float inAirAngularBrakeFactor = 0.99f;

    [TitleGroup(R)] Rigidbody rB;

    [TitleGroup(I)] private bool hasAirControlBehavior = false;
    [TitleGroup(R)] private InAirControllBehavior airControllBehavior = null;

    [TitleGroup(I)] private bool hasSteeringBehavior = false;
    [TitleGroup(R)] private SteeringBehavior steeringBehavior = null;

    [TitleGroup(I)] private bool hasMagnetBehavior = false;
    [TitleGroup(R)] private MagnetBehavior magnetBehavior = null;

    //------------------------ SETUP
    public override bool SetRequirements()
    {
        //Set the Air Controll Behavior(if it has it.)
        if (cC.HasBehavior<InAirControllBehavior>())
        {
            hasAirControlBehavior = true;
            airControllBehavior = cC.GetBehavior<InAirControllBehavior>();
        }

        //Set the Steering Behavior(if it has it.)
        if (cC.HasBehavior<SteeringBehavior>())
        {
            hasSteeringBehavior = true;
            steeringBehavior = cC.GetBehavior<SteeringBehavior>();
        }

        //Set the Magnet Behavior(if it has it.)
        if (cC.HasBehavior<MagnetBehavior>())
        {
            hasMagnetBehavior = true;
            magnetBehavior = cC.GetBehavior<MagnetBehavior>();
        }

        // SET THE RIGIDBODY.
        rB = cC.gameObject.GetComponent<Rigidbody>();
        if (rB == null)
        {
            rB = cC.gameObject.AddComponent<Rigidbody>();
        }

        //CHECK IF INITIALISATION WAS SUCCESSFULL
        if (hasAirControlBehavior == true && airControllBehavior == null) // wenn bool entspricht nicht der wirklichkeit
        {
            return false;
        }
        if (hasSteeringBehavior == true && steeringBehavior == null) // wenn bool entspricht nicht der wirklichkeit
        {
            return false;
        }
        if (hasMagnetBehavior == true && magnetBehavior == null) // wenn bool entspricht nicht der wirklichkeit
        {
            return false;
        }


        if (rB == null) //wenn einer der wichtigen Referenzen null ist
        {
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
        if (EnabledBehavior)
        {
            // InAirControl automatic brake
            if (hasAirControlBehavior)
            {
                if (cC.drivingStateInfo == DrivingState.InAir) // wenn das auto in der luft ist.
                {
                    if (hasMagnetBehavior && hasSteeringBehavior) // wenn es ein Steering und ein Magnetbehavior hat - muss steering inaktiv sein und der magnet inaktiv sein
                    {
                        if (steeringBehavior.SteerInputVal == Vector2.zero && !magnetBehavior.MagnetIsActive) 
                        {
                            // Reduce angular velocity
                            rB.BrakeAngularVelocity(inAirAngularBrakeFactor);
                        }
                    }
                    else // wenn es kein Behavior hat.
                    {
                        rB.BrakeAngularVelocity(inAirAngularBrakeFactor); // breake einfach immer in der luft.
                    }

                }
            }
        }

    }
}
