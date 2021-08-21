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
    [Tooltip("NOT USED ANYMORE")]
    public float maximumLowRideDistance = 2f; // The maximum length that the wheels can extend
    [Range(0f,1f)] public float lowRideStepSize = 0.1f; // the maximum percentage which the wheels move(lowRide) each frame. (based on the maximumLowRideDistance)
    public AnimationCurve powerCurve = AnimationCurve.Linear(0f,1f,1f,1f); // The maximum length that the wheels can extend
    [Range(0, 0.3f)] public float minGroundDistance = 0.1f;
    public bool allignStickToView = false;
    [Range(0,1f)] public float lowRideSideScale = 0f;


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

        // Init low ride distance; kp ob sinnvoll
        //foreach(Wheel wheel in Wheels)
        //{
        //    wheel.wheelCollider.suspensionDistance = maximumLowRideDistance;
        //}
    }


    void FixedUpdate()
    {
        Steer(steerValue,frontWheelR, frontWheelL, backWheelR, backWheelL);
        Thrust(thrustValue,frontWheelR, frontWheelL, backWheelR, backWheelL);
        LowRide(lowRideValue, minGroundDistance, powerCurve, lowRideStepSize,frontWheelR, frontWheelL, backWheelR, backWheelL);
        SetCenterOfMass(rB);

        //Debug.DrawLine(transform.position, transform.position + transform.forward * 5f, Color.black, 0.5f);
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

        switch(steeringMethod)
        {
            case SteeringMethod.FrontSteer:
            {
                _frontWheelL.wheelCollider.steerAngle = targetAngle;
                _frontWheelR.wheelCollider.steerAngle = targetAngle;
                break;
            }
            case SteeringMethod.BackSteer:
            {
                _backWheelL.wheelCollider.steerAngle = targetAngle;
                _backWheelR.wheelCollider.steerAngle = targetAngle;
                break;
            }
            case SteeringMethod.FourWheelSteer:
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

        switch(propulsionMethod)
        {
            case PropulsionMethod.FrontDrive:
            {
                _frontWheelL.wheelCollider.motorTorque = _strength * motorForce;
                _frontWheelR.wheelCollider.motorTorque = _strength * motorForce;
                break;
            }
            case PropulsionMethod.BackDrive:
            {
                _backWheelL.wheelCollider.motorTorque = _strength * motorForce;
                _backWheelR.wheelCollider.motorTorque = _strength * motorForce;
                break;
            }
            case PropulsionMethod.FourWheelDrive:
            {
                _frontWheelL.wheelCollider.motorTorque = _strength * motorForce;
                _frontWheelR.wheelCollider.motorTorque = _strength * motorForce;
                _backWheelL.wheelCollider.motorTorque = _strength * motorForce;
                _backWheelR.wheelCollider.motorTorque = _strength * motorForce;
                break;
            }
        } 
    }

    private void LowRide(Vector2 _strength, float _minGroundDistance, AnimationCurve _powerCurve, float _lowRideStepSize, Wheel _frontWheelR, Wheel _frontWheelL, Wheel _backWheelR, Wheel _backWheelL)
    {
        float strengthWheelFR, strengthWheelFL, strengthWheelBR, strengthWheelBL;

        if (allignStickToView)
        {
            // Forward-Vektor des Autos im Screen-Space
            Vector2 forwardScreenVector = (Camera.main.WorldToScreenPoint(transform.position + transform.forward) - Camera.main.WorldToScreenPoint(transform.position)).normalized;

            // x-input rausrechnen
            float forwardAngle = Vector2.Angle(Vector2.up, forwardScreenVector);
            _strength = Quaternion.Euler(0, 0, -forwardAngle) * _strength;
            _strength *= new Vector2(lowRideSideScale, 1f);
            _strength = Quaternion.Euler(0, 0, forwardAngle) * _strength;

            // Stick-Richtungsvektoren für Dot-Produkt berechnen (sind abhängig von Richtung des Autos)
            Vector2 vecFR = Quaternion.Euler(0, 0, -45f) * forwardScreenVector;
            Vector2 vecFL = Quaternion.Euler(0, 0, 45f) * forwardScreenVector;
            Vector2 vecBR = Quaternion.Euler(0, 0, -135f) * forwardScreenVector;
            Vector2 vecBL = Quaternion.Euler(0, 0, 135) * forwardScreenVector;

            // Dot-Produkt [0,1]
            strengthWheelFR = Mathf.Clamp01(Vector2.Dot(vecFR.normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelFL = Mathf.Clamp01(Vector2.Dot(vecFL.normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelBR = Mathf.Clamp01(Vector2.Dot(vecBR.normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelBL = Mathf.Clamp01(Vector2.Dot(vecBL.normalized, _strength.normalized)) * _strength.magnitude;
        }
        else
        {
            // Rechne x-input raus
            _strength *= new Vector2(lowRideSideScale, 1f);

            // nimmt das dot product (Skalarprodukt) vom InputVektor und  dem "Radpositions-Vektor" und clampt es auf eine range von 0 bis 1 (voher wars -1 bis 1), 
            // danach wird es mit der intensität des verschubs des sticks multipliziert um die staerke zu bestimmen
            strengthWheelFR = Mathf.Clamp01(Vector2.Dot(new Vector2(1f, 1f).normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelFL = Mathf.Clamp01(Vector2.Dot(new Vector2(-1f, 1f).normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelBR = Mathf.Clamp01(Vector2.Dot(new Vector2(1f, -1f).normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelBL = Mathf.Clamp01(Vector2.Dot(new Vector2(-1f, -1f).normalized, _strength.normalized)) * _strength.magnitude;

            
        }
        _frontWheelR.OffsetWheelGradually(_lowRideStepSize, strengthWheelFR, _minGroundDistance, _powerCurve);
        _frontWheelL.OffsetWheelGradually(_lowRideStepSize, strengthWheelFL, _minGroundDistance, _powerCurve);
        _backWheelR.OffsetWheelGradually(_lowRideStepSize, strengthWheelBR, _minGroundDistance, _powerCurve);
        _backWheelL.OffsetWheelGradually(_lowRideStepSize, strengthWheelBL, _minGroundDistance, _powerCurve);

    }





    // ----------------------------------------- Input Events -----------------------------------------


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
        // 1. Put up
        transform.position += new Vector3(0, 5, 0);
        // 2. rotate up
        transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
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
