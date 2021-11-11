using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Rigidbody))]
public class InAirControllBehavior : CarBehavior
{
    const string R = "References";
    const string S = "Settings";
    const string I = "Input";

    [TitleGroup(S)] public float maxAngularVelocity = 4f;
    [TitleGroup(S)] public float inAirControlForce = 7f;
    [TitleGroup(S)] public Vector2 airRollSpeedPitchRoll = new Vector2(0.1f, 0.1f);
    [TitleGroup(S)] public bool invertRollControls = true;


    [TitleGroup(I)] private bool hasSteeredWhileInAir;
    [TitleGroup(I)] public bool HasSteeredWhileInAir {get => hasSteeredWhileInAir;}


    [TitleGroup(R)] private Rigidbody rB;
    [TitleGroup(S)] private Vector2 inAirSteerInputVal;
    [TitleGroup(S)] public Vector2 InAirSteerInputVal { get => inAirSteerInputVal; private set => inAirSteerInputVal = value; }

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
            return true; //cant not work
        }
    }

    //------------------------ INPUT HANDLING
    public void OnSteer(InputValue inputValue)
    {
        InAirSteerInputVal = inputValue.Get<Vector2>();
    }

    //------------------------ BEHAVIOR
    public override void ExecuteBehavior(Func<bool> _shouldExecute)
    {
        InAirControl(InAirSteerInputVal, ref hasSteeredWhileInAir);
    }



    private void InAirControl(Vector2 _inAirSteeringAngle, ref bool _hasSteeredWhileInAir)
    {
        if (EnabledBehavior && cC.drivingStateInfo == DrivingState.InAir)
        {
            // invert controls
            if (invertRollControls)
                _inAirSteeringAngle.y *= -1f;

            // complex rotation (90deg) of the 2 Dimensional inputVector - used as rotationAxis
            Vector3 inputNormal = new Vector3(-_inAirSteeringAngle.y, 0f, _inAirSteeringAngle.x);


            if (inputNormal.magnitude > 0.3f) // wenn gelenkt wurde (0.3f ist dabei der threshold zur erkennung der lenkung in der luft) 
            {
                _hasSteeredWhileInAir = false;
            }

            Vector3 localTorqueAxis = new Vector3(-_inAirSteeringAngle.y, _inAirSteeringAngle.x, 0);
            Vector3 globalTorqueAxis = this.transform.TransformVector(localTorqueAxis);
            rB.AddTorque(globalTorqueAxis * inAirControlForce, ForceMode.Acceleration);

            //// wird jetz 3 mal im ganzen script berechnet aber egal :-)
            //RaycastHit hit;
            //if (Physics.Raycast(transform.position, transform.up, out hit))
            //{
            //    //Vector3.Cross

            //    // HIER WEITERMACHEN
            //}

            rB.ClampAngularVelocity(maxAngularVelocity);
        }
        else //resets the _hasSteeredWhileInAir bool.  - ganz unschoener code, vielleicht faellt dir dazu was besseres ein? (sobald man in einer "luftperiode"(grounded -> inAir -> grounded) einmal gelenkt hat, sollte er nichtmehr autoalignen, fuer die luftperiode)
        {
            _hasSteeredWhileInAir = true;
        }
    }
}
