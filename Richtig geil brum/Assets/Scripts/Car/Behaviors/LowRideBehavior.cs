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
    const string H = "Helper";


    [TitleGroup(S)] public bool UseLowRideFrontal = true;
    [TitleGroup(S)] public bool UseLowRideBack = true;
    [TitleGroup(S)] public bool invertLowRideControls = false;
    [TitleGroup(S)] public bool allignStickToView = false;
    [TitleGroup(S)] public bool simplifyLowRide = false;

    [TitleGroup(S)] [Range(0, 0.2f)] public float lowRideActivityDecreaseSpeed = 0.012f;
    [TitleGroup(S)] [Range(0, 1f)] public float lowRideSideScale = 0.3f;


    [TitleGroup(R)] private Wheel frontWheelR, frontWheelL, backWheelR, backWheelL;
    [TitleGroup(R)] private Wheel[] Wheels { get { return new Wheel[4] { frontWheelR, frontWheelL, backWheelR, backWheelL }; } }



    [TitleGroup(I)] private Vector2 lowRideInputVal;
    [TitleGroup(I)] public Vector2 LowRideInputVal { get => lowRideInputVal; private set => lowRideInputVal = value; }
    [TitleGroup(I)] private  LowRideActivity lowRideActivity = new LowRideActivity();
    [TitleGroup(I)] public LowRideActivity LowRideActivity { get => lowRideActivity; private set => lowRideActivity = value; }


    //------------------------ SETUP
    public override bool SetRequirements()
    {
        //Init LowRideActivity
        LowRideActivity = new LowRideActivity();

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
        LowRideActivity.SetLowRideActivity(LowRideInputVal, lowRideActivityDecreaseSpeed,invertLowRideControls);
        LowRide(LowRideInputVal, frontWheelR, frontWheelL, backWheelR, backWheelL);
    }

    private void LowRide(Vector2 _inputStrength, Wheel _frontWheelR, Wheel _frontWheelL, Wheel _backWheelR, Wheel _backWheelL)
    {
        float strengthWheelFR, strengthWheelFL, strengthWheelBR, strengthWheelBL;

        #region allign stick to view
        if (allignStickToView)
        {
            // Forward-Vektor des Autos im Screen-Space
            Vector2 forwardScreenVector = (Camera.main.WorldToScreenPoint(transform.position + transform.forward) - Camera.main.WorldToScreenPoint(transform.position)).normalized;

            // x-input rausrechnen
            float forwardAngle = Vector2.Angle(Vector2.up, forwardScreenVector);
            _inputStrength = Quaternion.Euler(0, 0, -forwardAngle) * _inputStrength;
            _inputStrength *= new Vector2(lowRideSideScale, 1f);
            _inputStrength = Quaternion.Euler(0, 0, forwardAngle) * _inputStrength;

            // Stick-Richtungsvektoren für Dot-Produkt berechnen (sind abhängig von Richtung des Autos)
            Vector2 vecFR = Quaternion.Euler(0, 0, -45f) * forwardScreenVector;
            Vector2 vecFL = Quaternion.Euler(0, 0, 45f) * forwardScreenVector;
            Vector2 vecBR = Quaternion.Euler(0, 0, -135f) * forwardScreenVector;
            Vector2 vecBL = Quaternion.Euler(0, 0, 135) * forwardScreenVector;

            // Dot-Produkt [0,1]
            strengthWheelFR = Mathf.Clamp01(Vector2.Dot(vecFR.normalized, _inputStrength.normalized)) * _inputStrength.magnitude;
            strengthWheelFL = Mathf.Clamp01(Vector2.Dot(vecFL.normalized, _inputStrength.normalized)) * _inputStrength.magnitude;
            strengthWheelBR = Mathf.Clamp01(Vector2.Dot(vecBR.normalized, _inputStrength.normalized)) * _inputStrength.magnitude;
            strengthWheelBL = Mathf.Clamp01(Vector2.Dot(vecBL.normalized, _inputStrength.normalized)) * _inputStrength.magnitude;
        }
        #endregion
        else
        {
            // Rechne x-input raus
            _inputStrength *= new Vector2(lowRideSideScale, 1f);

            // Invert controls
            if (invertLowRideControls)
            {
                _inputStrength *= new Vector2(1f, -1f);
            }

            if (!UseLowRideFrontal)
            {
                _inputStrength = new Vector2(_inputStrength.x, Mathf.Clamp(_inputStrength.y, -1f, 0f));
            }

            if (!UseLowRideBack)
            {
                _inputStrength = new Vector2(_inputStrength.x, Mathf.Clamp(_inputStrength.y, 0f, 1f));
            }

            // nimmt das dot product (Skalarprodukt) vom InputVektor und  dem "Radpositions-Vektor" und clampt es auf eine range von 0 bis 1 (voher wars -1 bis 1), 
            // danach wird es mit estimmen
            strengthWheelFR = Mathf.Clamp01(Vector2.Dot(new Vector2(0f, 1f).normalized, _inputStrength.normalized)) * _inputStrength.magnitude;
            strengthWheelFL = Mathf.Clamp01(Vector2.Dot(new Vector2(0f, 1f).normalized, _inputStrength.normalized)) * _inputStrength.magnitude;
            strengthWheelBR = Mathf.Clamp01(Vector2.Dot(new Vector2(0f, -1f).normalized, _inputStrength.normalized)) * _inputStrength.magnitude;
            strengthWheelBL = Mathf.Clamp01(Vector2.Dot(new Vector2(0f, -1f).normalized, _inputStrength.normalized)) * _inputStrength.magnitude;
        }

        _frontWheelL.TargetExtensionPercent = strengthWheelFL;
        _frontWheelR.TargetExtensionPercent = strengthWheelFR;
        _backWheelL.TargetExtensionPercent = strengthWheelBL;
        _backWheelR.TargetExtensionPercent = strengthWheelBR;

        //_frontWheelR.UpdateWheelHeight(_lowRideStepSizePlusMinus, strengthWheelFR, _minMaxGroundDistance, _powerCurve);
        //_frontWheelL.UpdateWheelHeight(_lowRideStepSizePlusMinus, strengthWheelFL, _minMaxGroundDistance, _powerCurve);
        //_backWheelR.UpdateWheelHeight(_lowRideStepSizePlusMinus, strengthWheelBR, _minMaxGroundDistance, _powerCurve);
        //_backWheelL.UpdateWheelHeight(_lowRideStepSizePlusMinus, strengthWheelBL, _minMaxGroundDistance, _powerCurve);
    }


}
public class LowRideActivity
{
    private float[] values = new float[2] {0f,0f};
    /// <summary>
    /// Front(0), back(1) Set-access via indexer of class-instance.
    /// </summary>
    public float[] Values
    { get { return values; } private set { values = value; } }

    public LowRideActivity() // empty constructor that inits the values.
    {
        values = new float[2] { 0f, 0f};
    }

    /// <summary>
    /// Set the lowRideActivity-variable, which is used to deactivate the magnetPower temporarily.
    /// </summary>
    /// <param name="_lowRideValue"></param>
    public void SetLowRideActivity(Vector2 _lowRideValue, float _lowRideActivityDecreaseSpeed = 0.01f, bool _invertLowRideInput = false)
    {
        // inverted controls
        if (_invertLowRideInput)
            _lowRideValue.y *= -1f;

        // (SCHEIß CODE) Alle 4 Richtungen der lowRideActivity erhöhen oder verringern 

        // front
        if (_lowRideValue.y > Values[0])
            Values[0] = _lowRideValue.y;                                    // set
        else
            Values[0] -= _lowRideActivityDecreaseSpeed;                     // decrease

        // back
        if (-_lowRideValue.y > Values[1])
            Values[1] = -_lowRideValue.y;                                   // set
        else
            Values[1] -= _lowRideActivityDecreaseSpeed;                     // decrease
    }

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
    public float this[CarDir _carDir]
    {
        get
        {
            switch (_carDir)
            {
                case CarDir.F:
                    return Values[0];
                case CarDir.B:
                    return Values[1];
            }

            return 0f;
        }
        set
        {
            float clampedVal = Mathf.Clamp01(value); // side-stick-bewegung erstmal ignorieren
            switch (_carDir)
            {
                case CarDir.F:
                    Values[0] = clampedVal;
                    break;
                case CarDir.B:
                    Values[1] = clampedVal;
                    break;
            }
        }
    }
}

public enum CarDir // man koennte dieses Enum benutzen um auf den Indexer zuzugreifen - ich bin mir da noch nicht sicher, aber das koennte es lesbarer machen :)
{ 
    F,
    B,
}