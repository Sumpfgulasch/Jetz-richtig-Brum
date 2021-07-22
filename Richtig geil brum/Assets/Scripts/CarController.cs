using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [Header("Settings")]
    public float maxSteerAngle = 30f;
    public float motorForce = 50;

    [Header("References")]
    public WheelCollider frontWheelColliderL;
    public WheelCollider frontWheelColliderR, backWheelColliderL, backWheelColliderR;
    public Transform frontWheelTransformL, frontWheelTransformR;
    public Transform backWheelTransformL, backWheelTransformR;

    private float thrustValue;
    private float steerValue;


    void Start()
    {
        
    }

    
    void FixedUpdate()
    {
        Steer(steerValue);
        Thrust(thrustValue);
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

    private void UpdateWheelPoses()
    {
        UpdateWheelPose(frontWheelColliderL, frontWheelTransformL);
        UpdateWheelPose(frontWheelColliderR, frontWheelTransformR);
        UpdateWheelPose(backWheelColliderL, backWheelTransformL);
        UpdateWheelPose(backWheelColliderR, backWheelTransformR);
    }

    private void UpdateWheelPose(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos = transform.position;
        Quaternion quat = transform.rotation;

        wheelCollider.GetWorldPose(out pos, out quat);

        wheelTransform.position = pos;
        wheelTransform.rotation = quat;
    }

    private void LowRide()
    {

    }



    // ----------------------------------------- Input -----------------------------------------



    public void OnThrust(InputValue inputValue)
    {
        float value = inputValue.Get<float>();
        thrustValue = value;

        //Thrust(value);
        //UpdateWheelPoses();
    }

    public void OnSteer(InputValue inputValue)
    {
        float value = inputValue.Get<float>();
        steerValue = value;

        //Steer(value);
        //UpdateWheelPoses();
    }

    public void OnLowRide(InputValue value)
    {
        // TO DO

        LowRide();
        UpdateWheelPoses();
    }
}
