using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using Sirenix.OdinInspector;

public class ThrustBehavior : CarBehavior
{
    const string R = "References";
    const string S = "Settings";
    const string I = "Input";

    [TitleGroup(S)] public float motorForce = 2500;

    [TitleGroup(S)] public PropulsionMethods propulsionMethod = PropulsionMethods.FourWheelDrive;

    [TitleGroup(S)] public bool UseDriveForward = true;
    [TitleGroup(S)] public bool UseDriveBackward = true;


    [TitleGroup(R)] private Wheel frontWheelR, frontWheelL, backWheelR, backWheelL;
    [TitleGroup(R)] private Wheel[] Wheels { get { return new Wheel[4] { frontWheelR, frontWheelL, backWheelR, backWheelL }; } }


    [TitleGroup(I)] private float thrustInputVal;
    [TitleGroup(I)] public float ThrustInputVal { get => thrustInputVal; private set => thrustInputVal = value; }

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
            Debug.LogWarning(this.transform.name + ": Thrust Behavior cant be executed Properly.");
            return false;
        }
        else
        {
            return true;
        }
    }

    //------------------------ INPUT HANDLING
    public void OnThrust(InputValue inputValue)
    {
        ThrustInputVal = inputValue.Get<float>();
    }

    //------------------------ BEHAVIOR


    public override void ExecuteBehavior(Func<bool> _shouldExecute)
    {
        if (_shouldExecute())
        { 
            Thrust(ThrustInputVal, frontWheelR, frontWheelL, backWheelR, backWheelL);
        }
    }

    private void Thrust(float _strength, Wheel _frontWheelR, Wheel _frontWheelL, Wheel _backWheelR, Wheel _backWheelL)
    {
        // brauchen wir hier evtl. deltatime?
        if (!UseDriveForward)
        {
            _strength = Mathf.Clamp(_strength, -1f, 0f);
        }
        if (!UseDriveBackward)
        {
            _strength = Mathf.Clamp01(_strength);
        }

        switch (propulsionMethod)
        {
            case PropulsionMethods.FrontDrive:
                {
                    _frontWheelL.wheelCollider.motorTorque = _strength * motorForce;
                    _frontWheelR.wheelCollider.motorTorque = _strength * motorForce;
                    break;
                }
            case PropulsionMethods.BackDrive:
                {
                    _backWheelL.wheelCollider.motorTorque = _strength * motorForce;
                    _backWheelR.wheelCollider.motorTorque = _strength * motorForce;
                    break;
                }
            case PropulsionMethods.FourWheelDrive:
                {
                    _frontWheelL.wheelCollider.motorTorque = _strength * motorForce;
                    _frontWheelR.wheelCollider.motorTorque = _strength * motorForce;
                    _backWheelL.wheelCollider.motorTorque = _strength * motorForce;
                    _backWheelR.wheelCollider.motorTorque = _strength * motorForce;
                    break;
                }
        }
    }
}

public enum PropulsionMethods
{
    FrontDrive,
    BackDrive,
    FourWheelDrive
}

