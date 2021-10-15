using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(TrajectoryRenderer))]
public class CarController : SerializedMonoBehaviour
{
    const string G = "General";
    const string M = "Modes";
    const string LR = "LowRide";
    const string R = "References";
    const string H = "Helper";
    const string MP = "MagnetPower";
    const string AA = "AutoAlign";


    [TitleGroup(G)] public float maxSteerAngle = 30f;
    [TitleGroup(G)] public float motorForce = 50;
    [TitleGroup(G)] public Vector2 airRollSpeedPitchRoll = Vector2.one;
    [TitleGroup(G),GUIColor(0.5f,0f,0f)] public Vector3 airRollCenterOffset = new Vector3(0f,0f,0f);
    [TitleGroup(G)] private Vector3 centerOfMassOffset = new Vector3(0f,0f,0f);
    [TitleGroup(G)][OdinSerialize] public Vector3 CenterOfMassOffset{get{return centerOfMassOffset;} set{centerOfMassOffset = value; SetCenterOfMass(rB, true);}}
    [TitleGroup(G)] public float maxSpeed = 20f;        // NOT PROPERLY USED; only for audio


    [TitleGroup(MP)] public int magnetPowerForce = 30;
    [TitleGroup(MP)] public ButtonMode magnetPowerButtonMode = ButtonMode.DeAndActivate;
    [TitleGroup(MP), Tooltip("Brake when magnetPower is active and the player doesn't accalerate")] public bool magnetPowerAutoBrake = true;
    [TitleGroup(MP), Range(0, 1f), ShowIf("magnetPowerAutoBrake")] public float magnetPowerBrakeFactor = 0.9f;
    [TitleGroup(MP)] public AnimationCurve magnetPowerDistanceCurve;


    [TitleGroup(AA)] public bool autoalignCarInAir = true;
    [TitleGroup(AA)] private AutoAlignSurface autoAlignSurface = AutoAlignSurface.Trajectory;
    [TitleGroup(AA)] [OdinSerialize] public AutoAlignSurface AutoAlignSurface 
    { 
        get 
        {
            return autoAlignSurface;
        }
        set
        {
            autoAlignSurface = value;

            if (trajectoryRenderer != null) // wenn ein trajectoryrenderer assinged ist
            {
                if (value != AutoAlignSurface.Trajectory) // wenn der AutoalignSurface bool umgeschaltet wird und nicht auf Trajectory steht
                {
                    trajectoryRenderer.ShowTrajectory = false; // schalte im trajectory das updaten aus
                    trajectoryRenderer.ClearTrajectory(); // loesche den bestehenden Trajectory im linerendeerer
                }
            
                else if (value == AutoAlignSurface.Trajectory) // wenn der AutoalignSurface bool umgeschaltet wird und auf Trajectory steht
                {
                    trajectoryRenderer.ShowTrajectory = true; // schalte im trajectory das updaten an
                }
            }
        } 
    }
    [TitleGroup(AA)] public RotationMethod autoAlignMethod = RotationMethod.Physics;
    [TitleGroup(AA)] public float maxAngularVelocity = 5f;
    [TitleGroup(AA)] public int autoAlignTorqueForce = 10;
    [TitleGroup(AA), Tooltip("Used to control the amount of torque force. x = 1 heißt dass Auto 100% aligned, x = 0 heißt dass Auto 90° gedreht, x = -1 dass 180° gedreht.")] 
    public AnimationCurve autoAlignAngleCurve;
    [TitleGroup(AA), Tooltip("Used to reduce the angular velocity when the car is aligned")] public AnimationCurve autoAlignBrakeCurve;
    [TitleGroup(AA)][OdinSerialize, Range(0f,0.2f), ShowIf("autoalignCarInAir"), ShowIf("autoAlignMethod", RotationMethod.SetRotation)] public float autoAlign_setRotationSpeed = 0.02f;
    [TitleGroup(AA)] public AnimationCurve autoAlignDistanceCurve;
    //[TitleGroup(AA), ShowIf("autoAlignSurface", AutoAlignSurface.Trajectory)] public AnimationCurve trajectorySpeedLerpCurve;
    [TitleGroup(AA)] public float inAirControlForce = 15f;
    [TitleGroup(AA), Range(0,1f)] public float inAirAngularBrakeFactor = 0.97f;
    

    [TitleGroup(M)] public PropulsionMethods propulsionMethod = PropulsionMethods.FrontDrive;
    [TitleGroup(M)] public SteeringMethods steeringMethod = SteeringMethods.FrontSteer;
    [TitleGroup(M)] public WheelOffsetModes wheelOffsetMode = WheelOffsetModes.SuspensionDistance;
    [TitleGroup(M)] public bool inAirCarControl = false;
    [TitleGroup(M),ShowIf("inAirCarControl")] public RotationMethod airControllType = RotationMethod.Physics;
    [TitleGroup(M), ShowIf("inAirCarControl")] public bool stopAutoaligningAfterInAirControl = true;
    [TitleGroup(M)] public bool simplifyLowRide = false;
    [TitleGroup(M)] public bool allignStickToView = false;
    [TitleGroup(M)] public bool invertRollControls = true;


    [TitleGroup(LR)] [MinMaxSlider(0f, 2.5f, true)]public Vector2 minMaxGroundDistance = new Vector2(0.1f, 1f);// The minimum/maximum length that the wheels can extend - minimum = x component || maximum = y component
    [TitleGroup(LR)] [VectorRange(0f,0.5f,-0.5f,0f,true)] public Vector2 lowRideStepSizePlusMinus = new Vector2(0.1f, -0.1f); // the maximum percentage which the wheels move(lowRide) each frame. (based on the maximumGroundDistance) - change when going positive = x component || change  when going negative = y component
    [TitleGroup(LR)] public AnimationCurve powerCurve = AnimationCurve.Linear(0f,1f,1f,1f); // The maximum length that the wheels can extend
    [TitleGroup(LR)] [Range(0,1f)] public float lowRideSideScale = 0f;


    [TitleGroup(R)] public Wheel frontWheelR, frontWheelL, backWheelR, backWheelL;
    [TitleGroup(R)] public Wheel[] Wheels {get{return new Wheel[4]{frontWheelR,frontWheelL,backWheelR,backWheelL};}}
    [TitleGroup(R), Required] public TrajectoryRenderer trajectoryRenderer = null;
    [TitleGroup(R), HideInInspector] public float thrustValue;
    [TitleGroup(R)] private Vector2 steerValue;
    [TitleGroup(R)] private Vector2 lowRideValue;
    [TitleGroup(R)] private Rigidbody rB;
    [TitleGroup(R)] public Rigidbody RB { get { return rB; } }
    [TitleGroup(R)] private bool shouldAutoAlign = true;
    [TitleGroup(R), ShowInInspector] private float inAirTime = 0f;
    [TitleGroup(R), ShowInInspector] private bool wheelsOut = false;
    [TitleGroup(R)] [ShowInInspector] public DrivingState drivingStateInfo {
        get
        {
            if (Wheels != null)
            {
                if (!Wheels.Contains(null))
                {
                    //int groundedWheels = 0;
                    foreach (Wheel wheel in Wheels)
                    {
                        WheelHit hit;
                        if (wheel.wheelCollider.GetGroundHit(out hit))
                        {
                            //groundedWheels++;
                            return DrivingState.Grounded;
                        }
                    }
                    //if (groundedWheels < 2)
                    return DrivingState.InAir;
                }
            }
            return DrivingState.Grounded;
        }
    }
    [TitleGroup(R)] public Material wheels_defaultMat;
    [TitleGroup(R)] public Material wheels_magnetPowerMat;
    [TitleGroup(R)] public MeshRenderer[] wheelsMeshes;

    [TitleGroup(H)] public bool showDebugHandles = true;
    [TitleGroup(H)] private bool autoAlignToSurfaceBool(){if(autoalignCarInAir){return true;}else{return false;}} // helperfunction for showif
    [TitleGroup(MP)] private IEnumerator magnetPowerRoutine;
    [TitleGroup(MP)] private bool magnetIsActive;
    [TitleGroup(MP)] private float targetSurfaceDistance;


    void Start() 
    {
        trajectoryRenderer = this.GetComponent<TrajectoryRenderer>();
        if (trajectoryRenderer == null)
        {
            trajectoryRenderer = this.gameObject.AddComponent<TrajectoryRenderer>();
        }

        rB = this.GetComponent<Rigidbody>();
        if (rB == null)
        {
            rB = this.gameObject.AddComponent<Rigidbody>();
        }

        InitSuspensionDistance();   
        SetCenterOfMass(rB);
    }

    void FixedUpdate()
    {
        SetAirTime(ref inAirTime);
        if (autoalignCarInAir)
        {
            AutoAlignCar(autoalignCarInAir, shouldAutoAlign, autoAlign_setRotationSpeed, drivingStateInfo);
        }
        Steer(steerValue,frontWheelR, frontWheelL, backWheelR, backWheelL, ref shouldAutoAlign);
        Thrust(thrustValue,frontWheelR, frontWheelL, backWheelR, backWheelL);
        LowRide(lowRideValue, minMaxGroundDistance, powerCurve, lowRideStepSizePlusMinus,frontWheelR, frontWheelL, backWheelR, backWheelL);

        // MagnetPower automatic brake
        if (magnetPowerAutoBrake)
        {
            if (magnetIsActive && thrustValue == 0 && drivingStateInfo != DrivingState.InAir)
            {
                Brake(magnetPowerBrakeFactor);
            }
        }
        // InAirControl automatic brake
        if (inAirCarControl)
        {
            if (drivingStateInfo == DrivingState.InAir)
            {
                if (steerValue == Vector2.zero && !magnetIsActive)
                {
                    // Reduce angular velocity
                    rB.angularVelocity *= inAirAngularBrakeFactor;
                }
            }
        }
        
        
    }

    // ----------------------------------------- Setup -----------------------------------------

    private void SetCenterOfMass(Rigidbody _rb, bool _setWhileEditor = false)
    {
        if(_rb == null )
        {
            if (_setWhileEditor == false) // quick hack to use this command in serialisation, without losing the ability to get the logwarning if it fails on runtime.
            { 
                Debug.LogWarning("Rigidbody ist null - es gibt keinen auf diesem Auto");
            }
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

            if (inputNormal.magnitude > 0.3f) // wenn gelenkt wurde (0.3f ist dabei der threshold zur erkennung der lenkung in der luft) 
            {
                if (stopAutoaligningAfterInAirControl) // wenn er nicht weiter autoalignen soll, sobald in der luft gelenkt wurde
                {
                    _shouldAutoAlign = false;
                }
            }

            //calculate the rotationspeed for different axis
            float xAlignmentFactor = Mathf.Abs(Vector3.Dot(inputNormal.normalized, Vector3.right)); //get the alignment factor of the input and the axis by dotting    -   absolute to incorporate left and right as a range between 0 and 1
            float yAlignmentFactor = Mathf.Abs(Vector3.Dot(inputNormal.normalized, Vector3.forward)); //get the alignment factor of the input and the axis by dotting    -   absolute to incorporate top and bottom as a range between 0 and 1

            float airRollSpeed = xAlignmentFactor * airRollSpeedPitchRoll.x + yAlignmentFactor * airRollSpeedPitchRoll.y;

            switch (airControllType)
            {
                case RotationMethod.SetRotation:
                    {
                        //rotate around the cars position --- around the inputAxis(which is rotated by the cars rotation) (meaning its now in local space) ---  with the inputspeed * a fixed multiplier
                        this.transform.RotateAround(this.transform.position, this.transform.rotation * inputNormal, airRollSpeed * _steeringAngle.magnitude); // direct control
                        break;
                    }
                case RotationMethod.Physics:
                    {
                        // gib torque entlang der input axis (world space)
                        rB.AddTorque(this.transform.rotation * new Vector3(_steeringAngle.y, 0f, -_steeringAngle.x).normalized * airRollSpeed * 100f, ForceMode.Acceleration); // Physics Approach - *10000 weil umrechnungsfactor von direkter steuerung zu physics
                        
                        // Sorry für Auskommentieren! Ist das gleiche wie deins! Find ich nur schöner zu lesen :-D. Und ich raff deine rotation-Rechnung nicht kannst du mir das erklären

                        var localTorqueAxis = new Vector3(_steeringAngle.y, _steeringAngle.x, 0);
                        var globalTorqueAxis = transform.TransformVector(localTorqueAxis);
                        rB.AddTorque(globalTorqueAxis * inAirControlForce, ForceMode.Acceleration);

                        // wird jetz 3 mal im ganzen script berechnet aber egal :-)
                        RaycastHit hit;
                        if (Physics.Raycast(transform.position, transform.up, out hit))
                        {
                            //Vector3.Cross

                            // HIER WEITERMACHEN
                        }

                        ClampAngularVelocity(maxAngularVelocity);
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

    private void AutoAlignCar(bool _autoalignCarInAir, bool _shouldAutoAlign,float _autoalignCarInAirSpeed, DrivingState _drivingStateInfo)
    {
        #region Autoalign Car in Air
        if(_autoalignCarInAir && _shouldAutoAlign && _drivingStateInfo == DrivingState.InAir)    //shouldAutoAlign is coupled with the Input of the car, so the car doesnt autoalign, against the airControl
        {
            Vector3 targetNormal = Vector3.up.normalized;
            RaycastHit hit;

            // Decide to which surface the car should align
            switch (AutoAlignSurface)
            {
                case AutoAlignSurface.LowerSurface:
                    {
                        
                        if (Physics.Raycast(this.transform.position, -this.transform.up, out hit))
                        {
                            // get the normal of the hit
                            targetNormal = hit.normal;
                            targetSurfaceDistance = (hit.point - transform.position).magnitude;
                        }
                        break;
                    }
                case AutoAlignSurface.Trajectory:
                    {
                        // Ggf. nötig: Hit unter Auto
                        if (Physics.Raycast(this.transform.position, -this.transform.up, out hit))
                        {
                            targetNormal = hit.normal;
                            targetSurfaceDistance = (hit.point - transform.position).magnitude;
                        }

                        if (trajectoryRenderer.trajectory.HasHit)
                        {
                            if (drivingStateInfo == DrivingState.InAir || lowRideValue != Vector2.zero || rB.velocity.magnitude < 3.5f)
                            {
                                targetNormal = trajectoryRenderer.trajectory.HitNormal;
                                targetSurfaceDistance = (trajectoryRenderer.trajectory.HitPoint - transform.position).magnitude;
                            }
                        }
                        break;
                    }
            }
            
            // the forward component of the transform mapped onto 2D (might fail when it is facing purly up or down
            Vector3 forwardNormal = forwardNormal = new Vector3(this.transform.forward.x, 0f,this.transform.forward.z).normalized;

            // the target rotation calculated through forward and up vectors.
            Quaternion targetQuaternion = Quaternion.LookRotation(forwardNormal,targetNormal);

            // Rotation Calculation
            switch (autoAlignMethod)
            {
                // V1: Set Rotation
                case RotationMethod.SetRotation:
                    {
                        rB.rotation = Quaternion.Slerp(rB.rotation, targetQuaternion, _autoalignCarInAirSpeed);
                        break;
                    }
                // V2: AddTorque & brake
                case RotationMethod.Physics:
                    {
                        AddTorqueAndBrake(targetNormal, autoAlignTorqueForce, maxAngularVelocity, targetSurfaceDistance, autoAlignDistanceCurve, autoAlignAngleCurve, autoAlignBrakeCurve);
                        break;
                    }
            }
            
            
            
        }
        #endregion
    }

    /// <summary>
    /// Reduce velocity of the rigidbody.
    /// </summary>
    /// <param name="rigid"></param>
    /// <param name="factor"></param>
    private void Brake(float factor)
    {
        rB.velocity *= factor;
    }

    private void ReduceAngularVelocity(float factor)
    {

    }

    /// <summary>
    /// Add a torque and brake if target rotation is achieved. Takes into consideration: distance to the targetSurface, angle difference to the targetSurfaceNormal
    /// </summary>
    private void AddTorqueAndBrake(Vector3 targetNormal, float torqueForce, float maxTorqueSpeed, float targetSurfaceDistance = 0, AnimationCurve torqueDistanceCurve = null, AnimationCurve torqueAngleCurve = null, AnimationCurve brakeDistanceCurve = null)
    {
        float distanceFactor = Mathf.Clamp01(torqueDistanceCurve.Evaluate(targetSurfaceDistance));                  // Wert zwischen 0 und 1; entscheidet ob torque und brake verrechnet wird
        float angleDotProduct = Vector3.Dot(transform.up, targetNormal);

        // 1. ADD TORQUE
        Vector3 torqueAxis = Vector3.Cross(transform.up, targetNormal).normalized;                                  // Rotations-Achse = Kreuzprodukt
        float torqueAngleFactor = torqueAngleCurve.Evaluate(angleDotProduct);                                       // Faktor je nach Übereinstimmung von Zielnormale und transform.up
        rB.AddTorque(torqueAxis * torqueForce * distanceFactor * torqueAngleFactor, ForceMode.Acceleration);

        // 2. BRAKE                            
        float velocityDistanceFactor = brakeDistanceCurve.Evaluate(Mathf.Clamp01(angleDotProduct));                 // Wert wird 1, wenn Distanz zu hoch (-> keine Veränderung), 0 wenn Distanz niedrig
        rB.angularVelocity *= Mathf.Lerp(1f, velocityDistanceFactor, distanceFactor);                               // Wenn keine Winkeldifferenz, dann angularVel *= 0; wenn Winkeldifferenz >= 90°, dann keine Veränderung

        // 3. Max speed
        ClampAngularVelocity(maxAngularVelocity);
        
    }

    /// <summary>
    /// Align car to a surface and add a force of the car to that surface.
    /// </summary>
    /// <returns></returns>
    private IEnumerator MagnetPower()
    {
        while (magnetIsActive)
        {
            AutoAlignCar(true, shouldAutoAlign, autoAlign_setRotationSpeed, DrivingState.InAir);
            AddPullForce();
            yield return null;
        }

    }

    private void ClampAngularVelocity(float max)
    {
        if (rB.angularVelocity.magnitude > max)
        {
            rB.angularVelocity = rB.angularVelocity.normalized * max;
        }
    }

    /// <summary>
    /// Scheiß funktion. Nochmal schön schreiben. Fügt dem Auto Force nach unten hinzu, abhängig von der Distanz der targetSurface.
    /// </summary>
    private void AddPullForce()
    {
        float surfaceDistance = 1000;

        // 1. Get vector pointing downwards from car
        Vector3 downVector = -transform.up;
        
        // 2. Get distance factor
        RaycastHit hit;                                                                     // scheiße mit extra raycast, geschieht schon in autoAlignment, aber eben nicht jeden frame...
        if (Physics.Raycast(transform.position, -this.transform.up, out hit))
        {
            surfaceDistance = (hit.point - transform.position).magnitude;
        }
        float distanceFactor = Mathf.Clamp01(magnetPowerDistanceCurve.Evaluate(surfaceDistance));

        // 3. Add force
        rB.AddForce(downVector * magnetPowerForce * distanceFactor, ForceMode.Acceleration);
        //rB.addfo
    }

    private void SetWheelsMaterial(Material material)
    {
        foreach(MeshRenderer wheel in wheelsMeshes)
        {
            wheel.material = material;
        }
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

    public void OnExtendWheels(InputValue inputValue)
    {
        if (wheelsOut)
        {

            wheelsOut = false;
        }
        else
        {
            wheelsOut = true;
        }
    }

    public void OnJump(InputValue inputValue)
    {

    }

    public void OnMagnetPower(InputValue inputValue)
    {
        // Button mode #1: De- & activate
        if (magnetPowerButtonMode == ButtonMode.DeAndActivate) 
        {
            if (inputValue.isPressed)
            {
                if (magnetIsActive)
                {
                    magnetIsActive = false;
                    StopCoroutine(MagnetPower());
                    SetWheelsMaterial(wheels_defaultMat);
                }
                else
                {
                    magnetIsActive = true;
                    StartCoroutine(MagnetPower());
                    SetWheelsMaterial(wheels_magnetPowerMat);
                }
            }
        }

        // Button mode #2: Pressed
        else if (magnetPowerButtonMode == ButtonMode.Hold)
        {
            if (inputValue.isPressed)
            {
                magnetIsActive = true;
                StartCoroutine(MagnetPower());
                SetWheelsMaterial(wheels_magnetPowerMat);
            }
            else
            {
                magnetIsActive = false;
                StopCoroutine(MagnetPower());
                SetWheelsMaterial(wheels_defaultMat);
            }
        }

        
    }

    public void OnReset(InputValue inputValue)
    {
        // 1. Put up
        transform.position += new Vector3(0, 5, 0);
        // 2. rotate up
        rB.MoveRotation(Quaternion.LookRotation(transform.forward, Vector3.up));
        rB.angularVelocity = Vector3.zero;
    }



}



// -----------------------------------------------------------------------------------------------------



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
    InAir,
    TwoWheelsGrounded
}
public enum WheelOffsetModes{
    TargetPosition,
    SuspensionDistance
}

public enum RotationMethod
{
    Physics,
    SetRotation
}

public enum ButtonMode
{
    DeAndActivate,          // press once to de- or activate
    Hold                    // hold to perform an action
}

public enum AutoAlignSurface
{
    LowerSurface,
    WorldUp,
    Trajectory
}


