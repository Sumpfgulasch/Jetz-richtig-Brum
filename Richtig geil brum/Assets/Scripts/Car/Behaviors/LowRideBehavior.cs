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
        if (!Wheels.Contains(null))
            return true;

        Debug.LogWarning(this.transform.name + ": LowRide Behavior cant be executed Properly.");
        return false;

    }
    //------------------------ INPUT HANDLING


    public void OnLowRide(InputValue inputValue)
    {
        if (EnabledBehavior)
        {
            LowRideInputVal = inputValue.Get<Vector2>();
        }
    }


    public void OnLowRideFront(InputValue inputValue)
    {
        if (!EnabledBehavior)
            return;

        LowRideInputVal = Vector2.up;
        StopCoroutine(ResetInputValue(0.2f));
        StartCoroutine(ResetInputValue(0.2f));
    }

    public void OnLowRideBack(InputValue inputValue)
    {
        if (!EnabledBehavior)
            return;

        LowRideInputVal = Vector2.down;
        StopCoroutine(ResetInputValue(0.2f));
        StartCoroutine(ResetInputValue(0.2f));
    }

    private IEnumerator ResetInputValue(float delay)
    {
        yield return new WaitForSeconds(delay);
        lowRideInputVal = Vector2.zero;
    }

    //------------------------ BEHAVIOR
    public override void ExecuteBehavior(Func<bool> _shouldExecute)
    {
        LowRideActivity.SetLowRideActivity(LowRideInputVal, lowRideActivityDecreaseSpeed, invertLowRideControls);
        LowRide(LowRideInputVal, frontWheelR, frontWheelL, backWheelR, backWheelL);
    }

    private void LowRide(Vector2 _inputStrength, Wheel _frontWheelR, Wheel _frontWheelL, Wheel _backWheelR, Wheel _backWheelL)
    {
        float strengthWheelFR, strengthWheelFL, strengthWheelBR, strengthWheelBL;
        _inputStrength *= Time.deltaTime * 60f;

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

            // Stick-Richtungsvektoren f�r Dot-Produkt berechnen (sind abh�ngig von Richtung des Autos)
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

public enum CarDir // man koennte dieses Enum benutzen um auf den Indexer zuzugreifen - ich bin mir da noch nicht sicher, aber das koennte es lesbarer machen :)
{
    F,
    B,
}