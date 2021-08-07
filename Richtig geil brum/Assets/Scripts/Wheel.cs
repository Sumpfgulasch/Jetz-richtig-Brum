using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Wheel : SerializedMonoBehaviour
{
    const string R = "References";


    [TitleGroup(R)]
    CarController carController;
    public WheelCollider wheelCollider;
    public Transform wheelModelTransform; // visual wheel model
    private Vector3 startingPos;
    public Vector3 StartingPos{
        get{ if(carController != null){return carController.transform.rotation * startingPos  + carController.transform.position;} else {return Vector3.zero;}} set{startingPos = value;}}
    private bool startingPosWheelIsSet = false;


    void Awake()
    {
        //SET WheelCollider
        wheelCollider = this.GetComponent<WheelCollider>();

        //Ask for Model
        if(wheelModelTransform == null)
        {
            Debug.LogWarning("there is no Model assigned to this wheel");
        }

        //SET CARCONTROLLER
        CarController[] carControllers = GameObject.FindObjectsOfType<CarController>();
        foreach (var cC in carControllers) // geh durch alle carController
        {
            if(this.transform.IsChildOf(cC.transform)) // wenn das ist child von carController
            {
                carController = cC;
                break;
            }
        }
        if(carController == null)
        {
            Debug.LogWarning("CarController Reference is not set");
        }

        //SET Starting pos
        SetStartingWheelPositions();
    }

    void Update() 
    {
        if (this.transform.hasChanged) // changes the Model when the collider changes
        {
            UpdateWheelPose();
            transform.hasChanged = false;
        }
    }

    private void SetStartingWheelPositions()
    {
        startingPos = this.transform.position - carController.transform.position;

        if(!startingPosWheelIsSet)
        {
            startingPosWheelIsSet = true;
        }
        else
        {
            Debug.LogWarning("startingPosWheelIsSet is already set to true");
        }
    }

    private void UpdateWheelPose()
    {
        //Vector3 pos = transform.position; // indem du in Zeile 68 ein "out" benutzt erstellst du in dem moment die Variable
        //Quaternion quat = transform.rotation;

        this.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion quat);

        this.wheelModelTransform.position = pos;
        this.wheelModelTransform.rotation = quat;
    }

    public void OffsetWheelGradually(float _maxDistance, float _stepSize, float _strength, AnimationCurve _powerCurve) //Pushes the Wheel towards a goal, by an step amount(so it has to be called multiple times to reach its goal)
    {
        // prozent der aktuellen position von startposition bis maximalposition
        float currentPercentLowRideDistance = (StartingPos - this.transform.position).magnitude / _maxDistance; 
        // prozent der goalposition
        float goalPercentLowRideDistance  = _maxDistance * _strength; 
        // geclampter schritt * _powerCurve multiplicator(based on currentPercent)
        float stepPercentLowRideDistance = Mathf.Clamp(goalPercentLowRideDistance - currentPercentLowRideDistance, -_stepSize, _stepSize) * _powerCurve.Evaluate(currentPercentLowRideDistance);
        // geclampte schrittweite + aktuelle prozentuale position = next percent offset
        float newPercentLowRideDistance = stepPercentLowRideDistance + currentPercentLowRideDistance;

        
        this.transform.position = StartingPos + (this.transform.up * _maxDistance * newPercentLowRideDistance);
    }

    public void OffsetWheel(float _maxDistance , float _strength) //immediately Sets the wheel position downwards by a percentage
    {
        // rad wird um seine startsposition +  eine maximaltiefe nach unten mit einer stärke verschoben.
        this.wheelCollider.transform.position = StartingPos + (this.transform.up * _maxDistance * _strength);
    }

}
