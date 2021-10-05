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
    [TitleGroup(G)] public Vector2 airRollSpeedPitchRoll = Vector2.one;
    [TitleGroup(G),GUIColor(0.5f,0f,0f)] public Vector3 airRollCenterOffset = new Vector3(0f,0f,0f);
    [TitleGroup(G)] private Vector3 centerOfMassOffset = new Vector3(0f,0f,0f);
    [TitleGroup(G)][OdinSerialize] public Vector3 CenterOfMassOffset{get{return centerOfMassOffset;} set{centerOfMassOffset = value; SetCenterOfMass(rB);}}
    [TitleGroup(G)][OdinSerialize, Range(0f,0.2f), ShowIf("autoalignCarInAir")] public float autoalignCarInAirSpeed = 0.1f;
    [TitleGroup(G)][OdinSerialize, ShowIf("autoAlignToSurfaceBool")] public float autoalignSurfaceDistance = 10f;


    [TitleGroup(M)] public PropulsionMethods propulsionMethod = PropulsionMethods.FrontDrive;
    [TitleGroup(M)] public SteeringMethods steeringMethod = SteeringMethods.FrontSteer;
    [TitleGroup(M)] public WheelOffsetModes wheelOffsetMode = WheelOffsetModes.SuspensionDistance;
    [TitleGroup(M),ShowIf("inAirCarControl")] public AirControllTypes airControllType = AirControllTypes.PureRotation;


    [TitleGroup(M)] public bool inAirCarControl = false;
    [TitleGroup(M)] public bool simplifyLowRide = false;
    [TitleGroup(M)] public bool allignStickToView = false;
    [TitleGroup(M)] public bool invertRollControls = true;
    [TitleGroup(M)] public bool autoalignCarInAir = true;
    [TitleGroup(M), ShowIf("autoalignCarInAir")] public bool autoalignToSurface = true;


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
    [TitleGroup(R)] private bool shouldAutoAlign = true;
    [TitleGroup(R), ShowInInspector] private float inAirTime = 0f;
    [TitleGroup(R), ShowInInspector] private bool wheelsOut = false;
    [TitleGroup(R)] [ShowInInspector] public DrivingState drivingStateInfo {
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
    [TitleGroup(H)] private bool autoAlignToSurfaceBool(){if(autoalignCarInAir && autoalignToSurface){return true;}else{return false;}} // helperfunction for showif


    void Start() {
        rB = this.GetComponent<Rigidbody>();
        InitSuspensionDistance();   
        SetCenterOfMass(rB);
    }

    void FixedUpdate()
    {
        SetAirTime(ref inAirTime);
        AutoAlignCar(autoalignCarInAir,autoalignToSurface,shouldAutoAlign,autoalignCarInAirSpeed,drivingStateInfo);
        Steer(steerValue,frontWheelR, frontWheelL, backWheelR, backWheelL, ref shouldAutoAlign);
        Thrust(thrustValue,frontWheelR, frontWheelL, backWheelR, backWheelL);
        LowRide(lowRideValue, minMaxGroundDistance, powerCurve, lowRideStepSizePlusMinus,frontWheelR, frontWheelL, backWheelR, backWheelL);
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

    private void Steer(Vector2 _steeringAngle, Wheel _frontWheelR, Wheel _frontWheelL, Wheel _backWheelR, Wheel _backWheelL, ref bool _shouldAutoAlign)
    {

        #region Steering on Ground
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
                _backWheelL.wheelCollider.steerAngle = -targetAngle;
                _backWheelR.wheelCollider.steerAngle = -targetAngle;
                break;
            }
        }
        #endregion


        #region Steering in Air
        if(inAirCarControl && drivingStateInfo == DrivingState.InAir) 
        {
            // complex rotation (90deg) of the 2 Dimensional inputVector - used as rotationAxis
            Vector3 inputNormal = new Vector3(-_steeringAngle.y, 0f,_steeringAngle.x);

            //flip the rotation Axis 180 deg so that the rotationangle will be opposite
            if(invertRollControls)
            {
                inputNormal *= -1f;
            }

            if(inputNormal.magnitude > 0.3f) // wenn gelenkt wurde (0.3f ist dabei der threshold zur erkennung der lenkung in der luft) 
            {
                _shouldAutoAlign = false;
            }

            //calculate the rotationspeed for different axis
            float xAlignmentFactor = Mathf.Abs(Vector3.Dot(inputNormal.normalized,Vector3.right)); //get the alignment factor of the input and the axis by dotting    -   absolute to incorporate left and right as a range between 0 and 1
            float yAlignmentFactor = Mathf.Abs(Vector3.Dot(inputNormal.normalized,Vector3.forward)); //get the alignment factor of the input and the axis by dotting    -   absolute to incorporate top and bottom as a range between 0 and 1

            float airRollSpeed = xAlignmentFactor * airRollSpeedPitchRoll.x + yAlignmentFactor * airRollSpeedPitchRoll.y;

            switch(airControllType)
            {
                case AirControllTypes.PureRotation:
                {
                    //rotate around the cars position --- around the inputAxis(which is rotated by the cars rotation) (meaning its now in local space) ---  with the inputspeed * a fixed multiplier
                    this.transform.RotateAround(this.transform.position,this.transform.rotation * inputNormal, airRollSpeed * _steeringAngle.magnitude); // direct control
                    break;
                }
                case AirControllTypes.PhysicsRotation:
                {
                    // gib torque entlang der input axis (world space)
                    rB.AddTorque(this.transform.rotation * new Vector3(_steeringAngle.y,0f,-_steeringAngle.x).normalized * airRollSpeed * 10000f, ForceMode.Force); // Physics Approach - *10000 weil umrechnungsfactor von direkter steuerung zu physics
                    break;
                }
            }


            }
        else //resets the shouldAutoAlign bool.  - ganz unschoener code, vielleicht faellt dir dazu was besseres ein? (sobald man in einer "luftperiode"(grounded -> inAir -> grounded) einmal gelenkt hat, sollte er nichtmehr autoalignen, fuer die luftperiode)
        {
            _shouldAutoAlign = true; 
        }
        #endregion
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
            // danach wird es mit estimmen
            strengthWheelFR = Mathf.Clamp01(Vector2.Dot(new Vector2(0f, 1f).normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelFL = Mathf.Clamp01(Vector2.Dot(new Vector2(0f, 1f).normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelBR = Mathf.Clamp01(Vector2.Dot(new Vector2(0f, -1f).normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelBL = Mathf.Clamp01(Vector2.Dot(new Vector2(0f, -1f).normalized, _strength.normalized)) * _strength.magnitude;

            
        }
        _frontWheelR.OffsetWheelGradually(_lowRideStepSizePlusMinus, strengthWheelFR, _minMaxGroundDistance, _powerCurve, wheelOffsetMode, wheelsOut);
        _frontWheelL.OffsetWheelGradually(_lowRideStepSizePlusMinus, strengthWheelFL, _minMaxGroundDistance, _powerCurve, wheelOffsetMode, wheelsOut);
        _backWheelR.OffsetWheelGradually(_lowRideStepSizePlusMinus, strengthWheelBR, _minMaxGroundDistance, _powerCurve, wheelOffsetMode, wheelsOut);
        _backWheelL.OffsetWheelGradually(_lowRideStepSizePlusMinus, strengthWheelBL, _minMaxGroundDistance, _powerCurve, wheelOffsetMode, wheelsOut);
    }

    private void SetAirTime(ref float _inAirTime)
    {
        //check if is on ground for autoalignCarInAir and controllinputinterference
        if(drivingStateInfo == DrivingState.InAir)
        {
            _inAirTime += Time.deltaTime;
        }
        else
        {
            _inAirTime = 0f;
        }
    }

    private void AutoAlignCar(bool _autoalignCarInAir, bool _autoalignToSurface, bool _shouldAutoAlign,float _autoalignCarInAirSpeed, DrivingState _drivingStateInfo)
    {
        #region Autoalign Car in Air
        if(_autoalignCarInAir && _shouldAutoAlign && _drivingStateInfo == DrivingState.InAir)    //shouldAutoAlign is coupled with the Input of the car, so the car doesnt autoalign, against the airControl
        {
            // can be set through Raycasting
            Vector3 upNormal = Vector3.up.normalized; 

            if(_autoalignToSurface) // override forwardNormal if alignToSurface is true and a hit was registered
            {
                RaycastHit hit;
                if (Physics.Raycast(this.transform.position, -this.transform.up, out hit, autoalignSurfaceDistance))
                {
                    // get the normal of the hit
                    upNormal = hit.normal;
                    //Debug.DrawRay(hit.point, hit.normal, Color.yellow,0.2f); // debug the hitpoint
                }
            }
            
            // the forward component of the transform mapped onto 2D (might fail when it is facing purly up or down
            Vector3 forwardNormal = forwardNormal = new Vector3(this.transform.forward.x, 0f,this.transform.forward.z).normalized;

            // the target rotation calculated through forward and up vectors.
            Quaternion targetQuaternion = Quaternion.LookRotation(forwardNormal,upNormal);

            //rotation Calculation
            rB.rotation = Quaternion.Slerp(rB.rotation, targetQuaternion, _autoalignCarInAirSpeed); // set the rotation via RB
            //this.transform.rotation = Quaternion.Slerp(this.transform.rotation, targetQuaternion, _autoalignCarInAirSpeed); // set the rotation via transform
        }
        #endregion
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
public enum AirControllTypes{
    PureRotation,
    PhysicsRotation
}
