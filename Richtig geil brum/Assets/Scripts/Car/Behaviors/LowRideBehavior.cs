using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using System.Linq;

public class LowRideBehavior : CarBehavior
{
    const string R = "References";
    const string S = "Settings";
    const string I = "Input";


    [TitleGroup(S)] public bool UseLowRideFrontal = true;
    [TitleGroup(S)] public bool UseLowRideBack = true;
    [TitleGroup(S)] public bool invertLowRideControls = true;
    [TitleGroup(S)] public bool allignStickToView = false;
    [TitleGroup(S)] public bool simplifyLowRide = false;

    [TitleGroup(S)] [MinMaxSlider(0f, 2.5f, true)] public Vector2 minMaxGroundDistance = new Vector2(0.15f, 1.35f);// The minimum/maximum length that the wheels can extend - minimum = x component || maximum = y component
    [TitleGroup(R)] private Vector2 curMinMaxGroundDistance = new Vector2();
    [TitleGroup(S)] [Range(0f, 2.5f)] public float extendedWheelsLowRideDistance = 1.2f;
    [TitleGroup(S)] [MinMaxSlider(0f, 1f, true)] public Vector2 extendWheelsTime = new Vector2(0.1f, 0.5f);
    [TitleGroup(R)] private List<Coroutine> extendWheelsRoutines = new List<Coroutine>();


    [TitleGroup(S)] [Range(0, 0.2f)] public float lowRideActivityDecreaseSpeed = 0.012f;
    [TitleGroup(S)] [VectorRange(0f, 0.5f, -0.5f, 0f, true)] public Vector2 lowRideStepSizePlusMinus = new Vector2(0.5f, -0.02f); // the maximum percentage which the wheels move(lowRide) each frame. (based on the maximumGroundDistance) - change when going positive = x component || change  when going negative = y component
    [TitleGroup(S)] public AnimationCurve powerCurve = AnimationCurve.Linear(0f, 1f, 1f, 2f); // The maximum length that the wheels can extend
    [TitleGroup(S)] public AnimationCurve lowRideActivityMagnetCurve = AnimationCurve.Linear(0f,1f,1f,0f);
    [TitleGroup(S)] public AnimationCurve lowRideActivityAlignCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [TitleGroup(S)] [Range(0, 1f)] public float lowRideSideScale = 0.3f;


    [TitleGroup(R)] private Wheel frontWheelR, frontWheelL, backWheelR, backWheelL;
    [TitleGroup(R)] private Wheel[] Wheels { get { return new Wheel[4] { frontWheelR, frontWheelL, backWheelR, backWheelL }; } }



    [TitleGroup(I)] private Vector2 lowRideInputVal;
    [TitleGroup(I)] public Vector2 LowRideInputVal { get => lowRideInputVal; private set => lowRideInputVal = value; }
    [TitleGroup(I)] private LowRideActivity lowRideActivity = new LowRideActivity();


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
            Debug.LogWarning(this.transform.name + ": LowRide Behavior cant be executed Properly.");
            return false;
        }
        else
        {
            return true;
        }
    }
    //------------------------ INPUT HANDLING
    public void OnLowRide(InputValue inputValue)
    {
        LowRideInputVal = inputValue.Get<Vector2>();
    }
    //------------------------ BEHAVIOR
    public override void ExecuteBehavior(Func<bool> _shouldExecute)
    {
        SetLowRideActivity(LowRideInputVal);
        LowRide(LowRideInputVal, curMinMaxGroundDistance, powerCurve, lowRideStepSizePlusMinus, frontWheelR, frontWheelL, backWheelR, backWheelL);
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
            if (invertLowRideControls)
            {
                _strength *= new Vector2(1f, -1f);
            }

            if (!UseLowRideFrontal)
            {
                _strength = new Vector2(_strength.x, Mathf.Clamp(_strength.y, -1f, 0f));
            }

            if (!UseLowRideBack)
            {
                _strength = new Vector2(_strength.x, Mathf.Clamp(_strength.y, 0f, 1f));
            }

            // nimmt das dot product (Skalarprodukt) vom InputVektor und  dem "Radpositions-Vektor" und clampt es auf eine range von 0 bis 1 (voher wars -1 bis 1), 
            // danach wird es mit estimmen
            strengthWheelFR = Mathf.Clamp01(Vector2.Dot(new Vector2(0f, 1f).normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelFL = Mathf.Clamp01(Vector2.Dot(new Vector2(0f, 1f).normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelBR = Mathf.Clamp01(Vector2.Dot(new Vector2(0f, -1f).normalized, _strength.normalized)) * _strength.magnitude;
            strengthWheelBL = Mathf.Clamp01(Vector2.Dot(new Vector2(0f, -1f).normalized, _strength.normalized)) * _strength.magnitude;


        }
        _frontWheelR.OffsetWheelGradually(_lowRideStepSizePlusMinus, strengthWheelFR, _minMaxGroundDistance, _powerCurve);
        _frontWheelL.OffsetWheelGradually(_lowRideStepSizePlusMinus, strengthWheelFL, _minMaxGroundDistance, _powerCurve);
        _backWheelR.OffsetWheelGradually(_lowRideStepSizePlusMinus, strengthWheelBR, _minMaxGroundDistance, _powerCurve);
        _backWheelL.OffsetWheelGradually(_lowRideStepSizePlusMinus, strengthWheelBL, _minMaxGroundDistance, _powerCurve);
    }

    /// <summary>
    /// Set the lowRideActivity-variable, which is used to deactivate the magnetPower temporarily.
    /// </summary>
    /// <param name="_lowRideValue"></param>
    private void SetLowRideActivity(Vector2 _lowRideValue)
    {
        // inverted controls
        if (invertLowRideControls)
            _lowRideValue.y *= -1f;

        // (SCHEIß CODE) Alle 4 Richtungen der lowRideActivity erhöhen oder verringern 

        // front
        if (_lowRideValue.y > lowRideActivity[0])
            lowRideActivity[0] = _lowRideValue.y;                                    // increase
        else
            lowRideActivity[0] -= lowRideActivityDecreaseSpeed;                     // decrease

        // right
        if (_lowRideValue.x > lowRideActivity[1])
            lowRideActivity[1] = _lowRideValue.x;
        else
            lowRideActivity[1] -= lowRideActivityDecreaseSpeed;

        // back
        if (-_lowRideValue.y > lowRideActivity[2])
            lowRideActivity[2] = -_lowRideValue.y;
        else
            lowRideActivity[2] -= lowRideActivityDecreaseSpeed;

        // left
        if (-_lowRideValue.x > lowRideActivity[3])
            lowRideActivity[3] = -_lowRideValue.x;
        else
            lowRideActivity[3] -= lowRideActivityDecreaseSpeed;
    }
}
public class LowRideActivity
{
    private float[] values = new float[4];
    /// <summary>
    /// Front(0), right(1), back(2), left(3). Set-access via indexer of class-instance.
    /// </summary>
    public float[] Values
    { get { return values; } private set { values = value; } }


    public bool IsActive
    {
        get
        {
            foreach (float value in Values)
            {
                if (value != 0)
                    return true;
            }
            return false;
        }
    }
    public float HighestValue
    {
        get
        {
            float highestValue = Values[0];
            foreach (float value in Values)
            {
                if (value > highestValue)
                    highestValue = value;
            }
            return highestValue;
        }
    }
    public float this[int i]
    {
        get
        {
            return Values[i];
        }
        set
        {
            if (i != 1 && i != 3)                       // side-stick-bewegung erstmal ignorieren
                Values[i] = Mathf.Clamp01(value);
        }
    }
}

public enum IsWheel // man koennte dieses Enum benutzen um auf den Indexer zuzugreifen - ich bin mir da noch nicht sicher, aber das koennte es lesbarer machen :)
{ 
    FL,
    FR,
    BL,
    BR
}