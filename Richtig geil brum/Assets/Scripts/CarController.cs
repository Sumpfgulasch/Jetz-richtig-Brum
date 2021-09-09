using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using System.Linq;

public class CarController : SerializedMonoBehaviour
{
    const string G = "General";
    const string M = "Modes";
    const string LR = "LowRide";
    const string R = "References";
    const string H = "Helper";


    [TitleGroup(G)] public float maxSteerAngle = 30f;
    [TitleGroup(G)] public float motorForce = 50;
    [TitleGroup(G)] public float airRollSpeed = 1f;
    [TitleGroup(G)] private Vector3 centerOfMassOffset = new Vector3(0f,0f,0f);
    [TitleGroup(G)][OdinSerialize] public Vector3 CenterOfMassOffset{get{return centerOfMassOffset;} set{centerOfMassOffset = value; SetCenterOfMass(rB);}}
    [TitleGroup(G)] public bool inAirCarControl = false;


    [TitleGroup(M)] public PropulsionMethods propulsionMethod = PropulsionMethods.FrontDrive;
    [TitleGroup(M)] public SteeringMethods steeringMethod = SteeringMethods.FrontSteer;
    [TitleGroup(M)] public WheelOffsetModes wheelOffsetMode = WheelOffsetModes.SuspensionDistance;


    [TitleGroup(M)] public bool allignStickToView = false;
    [TitleGroup(M)] public bool invertRollControls = true;


    [TitleGroup(LR)] [MinMaxSlider(0f, 2.5f, true)]public Vector2 minMaxGroundDistance = new Vector2(0.1f, 1f);// The minimum/maximum length that the wheels can extend - minimum = x component || maximum = y component
    [TitleGroup(LR)] [VectorRange(0f,0.5f,-0.5f,0f,true)] public Vector2 lowRideStepSizePlusMinus = new Vector2(0.1f, -0.1f); // the maximum percentage which the wheels move(lowRide) each frame. (based on the maximumGroundDistance) - change when going positive = x component || change  when going negative = y component
    [TitleGroup(LR)] public AnimationCurve powerCurve = AnimationCurve.Linear(0f,1f,1f,1f); // The maximum length that the wheels can extend
    [TitleGroup(LR)] [Range(0,1f)] public float lowRideSideScale = 0f;


    [TitleGroup(R)] public Wheel frontWheelR, frontWheelL, backWheelR, backWheelL;
    [TitleGroup(R)] public Wheel[] Wheels {get{return new Wheel[4]{frontWheelR,frontWheelL,backWheelR,backWheelL};}}
    [TitleGroup(R)] private float thrustValue;
    [TitleGroup(R)] private Vector2 steerValue;
    [TitleGroup(R)] private Vector2 lowRideValue;
    [TitleGroup(R)] private Rigidbody rB;
    [TitleGroup(R)] [ShowInInspector] DrivingState drivingStateInfo {
            get
            {
                if(Wheels != null)
                {
                    if(!Wheels.Contains(null))
                    {
                        foreach (Wheel wheel in Wheels)
                        {
                            WheelHit hit;
                            if(wheel.wheelCollider.GetGroundHit(out hit))
                            {
                                return DrivingState.Grounded;
                            }
                        }
                        return DrivingState.InAir;
                    }
                }
                return DrivingState.Grounded;
            }
        }

    [TitleGroup(H)] public bool showDebugHandles = true;

    private bool wheelsOut = false;


    void Start() {
        rB = this.GetComponent<Rigidbody>();
        InitSuspensionDistance();   
        SetCenterOfMass(rB);
    }

    void FixedUpdate()
    {

        Steer(steerValue,frontWheelR, frontWheelL, backWheelR, backWheelL);
        Thrust(thrustValue,frontWheelR, frontWheelL, backWheelR, backWheelL);
        LowRide(lowRideValue, minMaxGroundDistance, powerCurve, lowRideStepSizePlusMinus, frontWheelR, frontWheelL, backWheelR, backWheelL);
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

    public void InitSuspensionDistance()
    {
        if(Wheels != null)
        {
            foreach(Wheel wheel in Wheels)
            {
                if(wheel != null)
                {
                    wheel.wheelCollider.suspensionDistance = minMaxGroundDistance.y;
                }
            }
        }
    }
    private bool wheelsAreSet{get{if(Wheels.Contains(null)){return false;}else{return true;}}}
    [HideIf("wheelsAreSet")][Button("Find Wheels"), GUIColor(0f, 1f, 0f)] public void FindWheels()
    {
        GameObject wheelFR = GameObject.FindGameObjectWithTag("WheelFR");
        GameObject wheelFL = GameObject.FindGameObjectWithTag("WheelFL");
        GameObject wheelBR = GameObject.FindGameObjectWithTag("WheelBR");
        GameObject wheelBL = GameObject.FindGameObjectWithTag("WheelBL");


        if(wheelFR != null){Wheel w = wheelFR.GetComponent<Wheel>(); if(w != null){frontWheelR = w;}}
        if(wheelFL != null){Wheel w = wheelFL.GetComponent<Wheel>(); if(w != null){frontWheelL = w;}}
        if(wheelBR != null){Wheel w = wheelBR.GetComponent<Wheel>(); if(w != null){backWheelR = w;}}
        if(wheelBL != null){Wheel w = wheelBL.GetComponent<Wheel>(); if(w != null){backWheelL = w;}}
    } 

    // ----------------------------------------- Methods -----------------------------------------

    private void Steer(Vector2 _steeringAngle, Wheel _frontWheelR, Wheel _frontWheelL, Wheel _backWheelR, Wheel _backWheelL )
    {

        // brauchen wir hier evtl. deltatime? 
        float targetAngle = _steeringAngle.x * maxSteerAngle;

        switch(steeringMethod)
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
                _backWheelL.wheelCollider.steerAngle = targetAngle;
                _backWheelR.wheelCollider.steerAngle = targetAngle;
                break;
            }
        }
        
        if(inAirCarControl && drivingStateInfo == DrivingState.InAir) 
        {
            // complex rotation of the 2 Dimensional inputVector - used as rotationAxis
            Vector3 inputNormal = new Vector3(-_steeringAngle.y, 0f,_steeringAngle.x);
            //flip the rotation Axis 180 deg so that the rotationangle will be opposite
            if(invertRollControls)
            {
                inputNormal *= -1f;
            }

            //rotate around the cars position --- around the inputAxis(which is rotated by the cars rotation) (meaning its now in local space) ---  with the inputspeed * a fixed multiplier
            this.transform.RotateAround(this.transform.position,this.transform.rotation * inputNormal, airRollSpeed * _steeringAngle.magnitude); // direct control

            // Physics versuch scheint noch nicht zu funktionieren, vlt. ist Add Torque auch nicht das richtige dafuer
            //rB.AddTorque((Vector3)_steeringAngle * airRollSpeed, ForceMode.Force); // Physics Approach
        }
        

    }

    private void Thrust(float _strength, Wheel _frontWheelR, Wheel _frontWheelL, Wheel _backWheelR, Wheel _backWheelL )
    {
        // brauchen wir hier evtl. deltatime?

        switch(propulsionMethod)
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

    private void LowRide(Vector2 _strength, Vector2 _minMaxGroundDistance, AnimationCurve _powerCurve, Vector2 _lowRideStepSizePlusMinus, Wheel _frontWheelR, Wheel _frontWheelL, Wheel _backWheelR, Wheel _backWheelL)
    {
        float strengthWheelFR, strengthWheelFL, strengthWheelBR, strengthWheelBL;

        #region allign stick to view
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
        #endregion
        else
        {
            // Rechne x-input raus
            _strength *= new Vector2(lowRideSideScale, 1f);

            // Invert controls
            if (invertRollControls)
            {
                _strength *= new Vector2(1f, -1f);
            }

            // nimmt das dot product (Skalarprodukt) vom InputVektor und  dem "Radpositions-Vektor" und clampt es auf eine range von 0 bis 1 (voher wars -1 bis 1), 
            // danach wird es mit der intensität des verschubs des sticks multipliziert um die staerke zu bestimmen
            strengthWheelFR = Mathf.Clamp01(Vector2.Dot(new Vector2(0, 1f).normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelFL = Mathf.Clamp01(Vector2.Dot(new Vector2(0, 1f).normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelBR = Mathf.Clamp01(Vector2.Dot(new Vector2(0, -1f).normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelBL = Mathf.Clamp01(Vector2.Dot(new Vector2(0, -1f).normalized, _strength.normalized)) * _strength.magnitude;

        }
        _frontWheelR.OffsetWheelGradually(_lowRideStepSizePlusMinus, strengthWheelFR, minMaxGroundDistance, _powerCurve, wheelOffsetMode, wheelsOut);
        _frontWheelL.OffsetWheelGradually(_lowRideStepSizePlusMinus, strengthWheelFL, minMaxGroundDistance, _powerCurve, wheelOffsetMode, wheelsOut);
        _backWheelR.OffsetWheelGradually(_lowRideStepSizePlusMinus, strengthWheelBR, minMaxGroundDistance, _powerCurve, wheelOffsetMode, wheelsOut);
        _backWheelL.OffsetWheelGradually(_lowRideStepSizePlusMinus, strengthWheelBL, minMaxGroundDistance, _powerCurve, wheelOffsetMode, wheelsOut);

    }





    // ----------------------------------------- Input Events -----------------------------------------


    public void OnThrust(InputValue inputValue)
    {
        thrustValue = inputValue.Get<float>();
    }

    public void OnSteer(InputValue inputValue)
    {
        steerValue = inputValue.Get<Vector2>();
    }

    public void OnLowRide(InputValue inputValue)
    {
        lowRideValue = inputValue.Get<Vector2>();

        
    }

    public void OnJump(InputValue inputValue)
    {
        Debug.Log("Jump");
        
        if (wheelsOut)
        {

            wheelsOut = false;
        }
        else
        {
            wheelsOut = true;
        }
    }

    public void OnReset(InputValue inputValue)
    {
        // 1. Put up
        transform.position += new Vector3(0, 5, 0);
        // 2. rotate up
        transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
    }
}

public enum PropulsionMethods{
    FrontDrive,
    BackDrive,
    FourWheelDrive
}

public enum SteeringMethods{
    FrontSteer,
    BackSteer,
    FourWheelSteer
}
public enum DrivingState{
    Grounded,
    InAir
}
public enum WheelOffsetModes{
    TargetPosition,
    SuspensionDistance
}
