using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class WheelGripBehavior : CarBehavior
{
    const string R = "References";
    const string S = "Settings";
    const string I = "Input";
    const string O = "Output";

    [Tooltip("The sideways Stiffness(y-Value)(is parameter in Wheels) that gets set based on Speed(x-value).")]
    [TitleGroup(S)] public AnimationCurve sidewaysStiffnessBySpeed = AnimationCurve.EaseInOut(0f, 1f, 30f, 3f);
    [Tooltip("The force (y-Value), which is put on the car when a certain speed(x-value) is reached and the car is Grounded")]
    [TitleGroup(S)] public AnimationCurve downwardForceBySpeed = AnimationCurve.EaseInOut(0f, 0f, 30f, 5f);


    [TitleGroup(R)] Rigidbody rB = null;
    [TitleGroup(R)] private Wheel frontWheelR, frontWheelL, backWheelR, backWheelL;
    [TitleGroup(R)] private Wheel[] Wheels { get { return new Wheel[4] { frontWheelR, frontWheelL, backWheelR, backWheelL }; } }


    [TitleGroup(O)] [ReadOnly] public float currentPushdownEffect = 0f;
    [TitleGroup(O)] [ReadOnly] public float currentStiffnessEffect = 0f;

    //------------------------ SETUP
    public override bool SetRequirements()
    {

        //SETUP THE WHEELS
        frontWheelR = cC.frontWheelR != null ? cC.frontWheelR : null;
        frontWheelL = cC.frontWheelL != null ? cC.frontWheelL : null;
        backWheelR = cC.backWheelR != null ? cC.backWheelR : null;
        backWheelL = cC.backWheelL != null ? cC.backWheelL : null;

        // SET THE RIGIDBODY.
        rB = cC.gameObject.GetComponent<Rigidbody>();
        if (rB == null)
        {
            rB = cC.gameObject.AddComponent<Rigidbody>();
        }

        //CHECK IF INITIALISATION WAS SUCCESSFULL

        if (rB == null || Wheels.Contains(null)) // double check if it is still null, but that should never be the case.
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
        if (cC.drivingStateInfo == DrivingState.Grounded) // wenn das auto grounded ist ( grounded heisst?! keine ahnung.)
        {
            PushDownBasedOnSpeed();
        }

        AdjustStiffnessBasedOnSpeed(); // mach immer abhaengig von der geschwindigkeit, adjusten diese basespeed zeug.
    }
    private void AdjustStiffnessBasedOnSpeed() // can be used to prohibit Drifting
    {
        float speed = rB.velocity.magnitude;
        foreach (Wheel wheel in Wheels)
        {
            currentStiffnessEffect = sidewaysStiffnessBySpeed.Evaluate(speed);
            WheelFrictionCurve wC = wheel.wheelCollider.sidewaysFriction; // geht leider nicht in einer zeile.
            wC.stiffness = currentStiffnessEffect;
        }
    }
    private void PushDownBasedOnSpeed() // can be used to prohibit Drifting
    {
        float speed = rB.velocity.magnitude;
        currentPushdownEffect = downwardForceBySpeed.Evaluate(speed);
        rB.AddForceAtPosition(-this.transform.up * currentPushdownEffect, this.transform.position); // druecke es nach unten
    }
}
