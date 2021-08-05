using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class CarController : SerializedMonoBehaviour
{
    const string S = "Settings";
    const string R = "References";
    const string H = "Helper";


    [TitleGroup(S)]
    public float maxSteerAngle = 30f;
    public float motorForce = 50;
    public float maximumLowRideDistance = 2f; // The maximum length that the wheels can extend


    [TitleGroup(R)]
    public WheelCollider frontWheelColliderL, frontWheelColliderR, backWheelColliderL, backWheelColliderR;
    public Transform frontWheelTransformL, frontWheelTransformR;
    public Transform backWheelTransformL, backWheelTransformR;

    private float thrustValue;
    private float steerValue;
    private Vector2 lowRideValue;


    private Vector3 startingPosFrontWheelL,startingPosFrontWheelR,startingPosBackWheelL,startingPosBackWheelR;
    public Vector3 StartingPosFrontWheelL{get{return  this.transform.rotation * startingPosFrontWheelL  + this.transform.position;} set{startingPosFrontWheelL = value;}}
    public Vector3 StartingPosFrontWheelR{get{return this.transform.rotation * startingPosFrontWheelR + this.transform.position;} set{startingPosFrontWheelR = value;}}
    public Vector3 StartingPosBackWheelL{get{return this.transform.rotation * startingPosBackWheelL + this.transform.position;} set{startingPosBackWheelL = value;}}
    public Vector3 StartingPosBackWheelR{get{return this.transform.rotation * startingPosBackWheelR + this.transform.position;} set{startingPosBackWheelR = value;}}
    private bool startingPosWheelIsSet = false;


    [TitleGroup(H)]
    public bool showDebugHandles = true;


    void Start()
    {

        SetStartingWheelPositions();
    }

    
    void FixedUpdate()
    {
        Steer(steerValue);
        Thrust(thrustValue);
        LowRide(lowRideValue);
        UpdateWheelPoses();
    }



    // ----------------------------------------- Methods -----------------------------------------

    private void Steer(float steeringAngle)
    {
        float targetAngle = steeringAngle * maxSteerAngle;

        frontWheelColliderL.steerAngle = targetAngle;
        frontWheelColliderR.steerAngle = targetAngle;
    }

    private void Thrust(float strength)
    {
        frontWheelColliderL.motorTorque = strength * motorForce;
        frontWheelColliderR.motorTorque = strength * motorForce;
    }

    private void LowRide(Vector2 strength)
    {
        float strengthWheelFL = Mathf.Clamp01(Vector2.Dot(new Vector2(-1f,1f).normalized, strength.normalized)) * strength.magnitude;
        float strengthWheelFR = Mathf.Clamp01(Vector2.Dot(new Vector2(1f,1f).normalized, strength.normalized)) * strength.magnitude;
        float strengthWheelBL = Mathf.Clamp01(Vector2.Dot(new Vector2(-1f,-1f).normalized, strength.normalized)) * strength.magnitude;
        float strengthWheelBR = Mathf.Clamp01(Vector2.Dot(new Vector2(1f,-1f).normalized, strength.normalized)) * strength.magnitude;

        frontWheelColliderL.transform.position = StartingPosFrontWheelL + (-this.transform.up * maximumLowRideDistance * strengthWheelFL);
        frontWheelColliderR.transform.position = StartingPosFrontWheelR + (-this.transform.up * maximumLowRideDistance * strengthWheelFR);
        backWheelColliderL.transform.position = StartingPosBackWheelL + (-this.transform.up * maximumLowRideDistance * strengthWheelBL);
        backWheelColliderR.transform.position = StartingPosBackWheelR + (-this.transform.up * maximumLowRideDistance * strengthWheelBR);
    }

    private void UpdateWheelPoses()
    {
        UpdateWheelPose(frontWheelColliderL, frontWheelTransformL);
        UpdateWheelPose(frontWheelColliderR, frontWheelTransformR);
        UpdateWheelPose(backWheelColliderL, backWheelTransformL);
        UpdateWheelPose(backWheelColliderR, backWheelTransformR);
    }

    private void UpdateWheelPose(WheelCollider wheelCollider, Transform wheelTransform)
    {
        //Vector3 pos = transform.position; // indem du in Zeile 68 ein "out" benutzt erstellst du in dem moment die Variable
        //Quaternion quat = transform.rotation;

        wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion quat);

        wheelTransform.position = pos;
        wheelTransform.rotation = quat;
    }



    // ----------------------------------------- Input -----------------------------------------


    public void OnThrust(InputValue inputValue)
    {
        thrustValue = inputValue.Get<float>();

        //Thrust(value);
        //UpdateWheelPoses();
    }

    public void OnSteer(InputValue inputValue)
    {
        steerValue = inputValue.Get<float>();

        //Steer(value);
        //UpdateWheelPoses();
    }

    public void OnLowRide(InputValue inputValue)
    {
        lowRideValue = inputValue.Get<Vector2>();
        //UpdateWheelPoses();
    }



    // ----------------------------------------- Helper -----------------------------------------



    private void SetStartingWheelPositions()
    {
        StartingPosFrontWheelL = frontWheelColliderL.transform.position - this.transform.position;
        StartingPosFrontWheelR = frontWheelColliderR.transform.position - this.transform.position;
        StartingPosBackWheelL = backWheelColliderL.transform.position - this.transform.position;
        StartingPosBackWheelR = backWheelColliderR.transform.position - this.transform.position;

        if(!startingPosWheelIsSet)
        {
            startingPosWheelIsSet = true;
        }
        else
        {
            Debug.LogWarning("startingPosWheelIsSet is already set to true");
        }
    }
}
