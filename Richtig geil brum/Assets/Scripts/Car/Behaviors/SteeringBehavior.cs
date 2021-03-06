using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;


public class SteeringBehavior : CarBehavior
{
    const string R = "References";
    const string S = "Settings";
    const string I = "Input";

    [TitleGroup(S)] public float maxSteerAngle = 30f;
    [TitleGroup(S)] public SteeringMethods steeringMethod = SteeringMethods.FrontSteer;

    [TitleGroup(I)] private bool useAlternativeValues = false;
    [TitleGroup(I)] public bool UseAlternativeValues 
    { 
        get => useAlternativeValues; 
        set { 
            useAlternativeValues = value; 
            ResetWheelPositions(value == true?alternativeSteeringMethod:steeringMethod, frontWheelR,frontWheelL,backWheelR,backWheelL);  // decides wheather or not he should use alternativeSteeringMethod or steering Method as an input.
        } 
    }
    [TitleGroup(S)] public float alternativeMaxSteerAngle = 50f;
    [TitleGroup(S)] public SteeringMethods alternativeSteeringMethod = SteeringMethods.FourWheelSteer;

    [TitleGroup(S)] public bool UseSteerLeft = true;
    [TitleGroup(S)] public bool UseSteerRight = true;


    [TitleGroup(R)] private Wheel frontWheelR, frontWheelL, backWheelR, backWheelL;
    [TitleGroup(R)] private Wheel[] Wheels { get { return new Wheel[4] { frontWheelR, frontWheelL, backWheelR, backWheelL }; } }


    [TitleGroup(I)] private Vector2 steerInputVal;
    [TitleGroup(I)] public Vector2 SteerInputVal { get => steerInputVal; }
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
            Debug.LogWarning(this.transform.name + ": Steering Behavior cant be executed Properly.");
            return false;
        }
        else
        {
            return true;
        }
    }

    //------------------------ INPUT HANDLING
    public void OnSteer(InputValue inputValue)
    {
        steerInputVal = inputValue.Get<Vector2>();
    }
    public void OnFourWheelsSteer(InputValue inputValue)
    {
        if (inputValue.isPressed)
        {
            UseAlternativeValues = true;
        }
        else
        {
            UseAlternativeValues = false;
        }
    }

    //------------------------ BEHAVIOR
    public override void ExecuteBehavior(Func<bool> _shouldExecute)
    {
        if (UseAlternativeValues)
        {
            Steer(steerInputVal,alternativeSteeringMethod,alternativeMaxSteerAngle, frontWheelR, frontWheelL, backWheelR, backWheelL);
        }
        else
        {
            Steer(steerInputVal, steeringMethod, maxSteerAngle, frontWheelR, frontWheelL, backWheelR, backWheelL);
        }
    }

    private void Steer(Vector2 _steeringAngle, SteeringMethods _steeringMethod, float _maxSteerAngle, Wheel _frontWheelR, Wheel _frontWheelL, Wheel _backWheelR, Wheel _backWheelL)
    {
        // brauchen wir hier evtl. deltatime? 
        float targetAngle = _steeringAngle.x * _maxSteerAngle;
        if (!UseSteerLeft)
        {
            targetAngle = Mathf.Clamp(targetAngle, -360f, 0f);
        }
        if (!UseSteerRight)
        {
            targetAngle = Mathf.Clamp(targetAngle, 0f, 360f);
        }

        switch (_steeringMethod)
        {
            case SteeringMethods.FrontSteer:
                {
                    _frontWheelL.wheelCollider.steerAngle = targetAngle;
                    _frontWheelR.wheelCollider.steerAngle = targetAngle;
                    break;
                }
            case SteeringMethods.BackSteer:
                {
                    _backWheelL.wheelCollider.steerAngle = targetAngle;
                    _backWheelR.wheelCollider.steerAngle = targetAngle;
                    break;
                }
            case SteeringMethods.FourWheelSteer:
                {
                    _frontWheelL.wheelCollider.steerAngle = targetAngle;
                    _frontWheelR.wheelCollider.steerAngle = targetAngle;
                    _backWheelL.wheelCollider.steerAngle = -targetAngle;
                    _backWheelR.wheelCollider.steerAngle = -targetAngle;
                    break;
                }
        }
    }

    /// <summary>
    /// Resette immer die Raeder die durch die Aktuelle SteeringPosition nicht mehr beeinflusst werden koennen auf rotation winkel 0
    /// </summary>
    /// <param name="_currentSteeringMethod"></param>

    public void ResetWheelPositions(SteeringMethods _currentSteeringMethod, Wheel _frontWheelR, Wheel _frontWheelL, Wheel _backWheelR, Wheel _backWheelL)
    {
        switch (_currentSteeringMethod)
        {
            case SteeringMethods.FrontSteer: // hinterraeder koennen nichtmehr beeinflusst werden
                _backWheelL.wheelCollider.steerAngle = 0f;
                _backWheelR.wheelCollider.steerAngle = 0f;
                break;
            case SteeringMethods.BackSteer: // vorderraeder koennen nichtmehr beeinflusst werden
                _frontWheelL.wheelCollider.steerAngle = 0f;
                _frontWheelR.wheelCollider.steerAngle = 0f;
                break;
            case SteeringMethods.FourWheelSteer: //alle raeder koennen beeinflusst werden, also mach nix.
                break;
        }
    }
}
public enum SteeringMethods
{
    FrontSteer,
    BackSteer,
    FourWheelSteer
}