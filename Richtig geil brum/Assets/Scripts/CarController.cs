using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

public class CarController : SerializedMonoBehaviour
{
    const string S = "Settings";
    const string R = "References";
    const string H = "Helper";


    [TitleGroup(S)]
    public Vector3 centerOfMassOffset = new Vector3(0f,0f,0f);
    public PropulsionMethod propulsionMethod = PropulsionMethod.FrontDrive;
    public SteeringMethod steeringMethod = SteeringMethod.FrontSteer;
    public float maxSteerAngle = 30f;
    public float motorForce = 50;
    public float maximumLowRideDistance = 2f; // The maximum length that the wheels can extend
    [Range(0f,1f)] public float lowRideStepSize = 0.1f; // the maximum percentage which the wheels move(lowRide) each frame. (based on the maximumLowRideDistance)
    public AnimationCurve powerCurve = AnimationCurve.Linear(0f,1f,1f,1f); // The maximum length that the wheels can extend


    [TitleGroup(R)]
    public Wheel frontWheelR, frontWheelL, backWheelR, backWheelL;
    public Wheel[] Wheels {get{return new Wheel[4]{frontWheelR,frontWheelL,backWheelR,backWheelL};}}

    private float thrustValue;
    private float steerValue;
    private Vector2 lowRideValue;

    private Rigidbody rB;


    [TitleGroup(H)]
    public bool showDebugHandles = true;

    void Start() {
        rB = this.GetComponent<Rigidbody>();
    }
    void FixedUpdate()
    {
        Steer(steerValue,frontWheelR, frontWheelL, backWheelR, backWheelL);
        Thrust(thrustValue,frontWheelR, frontWheelL, backWheelR, backWheelL);
        LowRide(lowRideValue,maximumLowRideDistance, powerCurve, lowRideStepSize,frontWheelR, frontWheelL, backWheelR, backWheelL);
        SetCenterOfMass(rB);
    }

    // ----------------------------------------- Setup -----------------------------------------

    private void SetCenterOfMass(Rigidbody _rb)
    {
        if(_rb == null)
        {
            Debug.LogWarning("Rigidbody ist null - es gibt keinen auf diesem Auto");
            return;
        }
        _rb.centerOfMass =  centerOfMassOffset;
    }

    // ----------------------------------------- Methods -----------------------------------------

    private void Steer(float _steeringAngle, Wheel _frontWheelR, Wheel _frontWheelL, Wheel _backWheelR, Wheel _backWheelL )
    {
        // brauchen wir hier evtl. deltatime? 
        float targetAngle = _steeringAngle * maxSteerAngle;

        switch(propulsionMethod)
        {
            case PropulsionMethod.FrontDrive:
            {
                _frontWheelL.wheelCollider.steerAngle = targetAngle;
                _frontWheelR.wheelCollider.steerAngle = targetAngle;
                break;
            }
            case PropulsionMethod.BackDrive:
            {
                _backWheelL.wheelCollider.steerAngle = targetAngle;
                _backWheelR.wheelCollider.steerAngle = targetAngle;
                break;
            }
            case PropulsionMethod.FourWheelDrive:
            {
                _frontWheelL.wheelCollider.steerAngle = targetAngle;
                _frontWheelR.wheelCollider.steerAngle = targetAngle;
                _backWheelL.wheelCollider.steerAngle = targetAngle;
                _backWheelR.wheelCollider.steerAngle = targetAngle;
                break;
            }
        }

    }

    private void Thrust(float _strength, Wheel _frontWheelR, Wheel _frontWheelL, Wheel _backWheelR, Wheel _backWheelL )
    {
        // brauchen wir hier evtl. deltatime?

        switch(steeringMethod)
        {
            case SteeringMethod.FrontSteer:
            {
                _frontWheelL.wheelCollider.motorTorque = _strength * motorForce;
                _frontWheelR.wheelCollider.motorTorque = _strength * motorForce;
                break;
            }
            case SteeringMethod.BackSteer:
            {
                _backWheelL.wheelCollider.motorTorque = _strength * motorForce;
                _backWheelR.wheelCollider.motorTorque = _strength * motorForce;
                break;
            }
            case SteeringMethod.FourWheelSteer:
            {
                _frontWheelL.wheelCollider.motorTorque = _strength * motorForce;
                _frontWheelR.wheelCollider.motorTorque = _strength * motorForce;
                _backWheelL.wheelCollider.motorTorque = _strength * motorForce;
                _backWheelR.wheelCollider.motorTorque = _strength * motorForce;
                break;
            }
        } 
    }

    private void LowRide(Vector2 _strength, float _maximumLowRideDistance, AnimationCurve _powerCurve, float _lowRideStepSize, Wheel _frontWheelR, Wheel _frontWheelL, Wheel _backWheelR, Wheel _backWheelL)
    {
        // nimmt das dot product (Skalarprodukt) vom InputVektor und  dem "Radpositions-Vektor" und clampt es auf eine range von 0 bis 1 (voher wars -1 bis 1), 
        // danach wird es mit der intensität des verschubs des sticks multipliziert um die staerke zu bestimmen
        float strengthWheelFR = Mathf.Clamp01(Vector2.Dot(new Vector2(1f,1f).normalized, _strength.normalized)) * _strength.magnitude;
        float strengthWheelFL = Mathf.Clamp01(Vector2.Dot(new Vector2(-1f,1f).normalized, _strength.normalized)) * _strength.magnitude;
        float strengthWheelBR = Mathf.Clamp01(Vector2.Dot(new Vector2(1f,-1f).normalized, _strength.normalized)) * _strength.magnitude;
        float strengthWheelBL = Mathf.Clamp01(Vector2.Dot(new Vector2(-1f,-1f).normalized, _strength.normalized)) * _strength.magnitude;

        _frontWheelR.OffsetWheelGradually(_maximumLowRideDistance,_lowRideStepSize,strengthWheelFR,_powerCurve);
        _frontWheelL.OffsetWheelGradually(_maximumLowRideDistance,_lowRideStepSize,strengthWheelFL,_powerCurve);
        _backWheelR.OffsetWheelGradually(_maximumLowRideDistance,_lowRideStepSize,strengthWheelBR,_powerCurve);
        _backWheelL.OffsetWheelGradually(_maximumLowRideDistance,_lowRideStepSize,strengthWheelBL,_powerCurve);

        // _frontWheelR.OffsetWheel(_maximumLowRideDistance,strengthWheelFR);
        // _frontWheelL.OffsetWheel(_maximumLowRideDistance,strengthWheelFR);
        // _backWheelR.OffsetWheel(_maximumLowRideDistance,strengthWheelBR);
        // _backWheelL.OffsetWheel(_maximumLowRideDistance,strengthWheelBL);
    }





    // ----------------------------------------- Input -----------------------------------------


    public void OnThrust(InputValue inputValue)
    {
        thrustValue = inputValue.Get<float>();
    }

    public void OnSteer(InputValue inputValue)
    {
        steerValue = inputValue.Get<float>();
    }

    public void OnLowRide(InputValue inputValue)
    {
        lowRideValue = inputValue.Get<Vector2>();
    }

    public void OnJump(InputValue inputValue)
    {
        Debug.Log("Jump");
    }

    public void OnReset(InputValue inputValue)
    {
        Debug.Log("Reset");
    }
}

public enum PropulsionMethod{
    FrontDrive,
    BackDrive,
    FourWheelDrive
}

public enum SteeringMethod{
    FrontSteer,
    BackSteer,
    FourWheelSteer
}
