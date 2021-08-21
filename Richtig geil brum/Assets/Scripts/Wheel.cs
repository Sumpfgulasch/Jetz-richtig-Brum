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

    public void OffsetWheelGradually(float _stepSize, float _strength, float minGroundDistance, AnimationCurve _powerCurve) //Pushes the Wheel towards a goal, by an step amount(so it has to be called multiple times to reach its goal)
    {
        #region OLD: offset wheel transform
        //// prozent der aktuellen position von startposition bis maximalposition
        //float currentPercentLowRideDistance = (StartingPos - this.transform.position).magnitude / _maxDistance; 
        //// prozent der goalposition
        //float goalPercentLowRideDistance  = _maxDistance * _strength; 
        //// geclampter schritt * _powerCurve multiplicator(based on currentPercent)
        //float stepPercentLowRideDistance = Mathf.Clamp(goalPercentLowRideDistance - currentPercentLowRideDistance, -_stepSize, _stepSize) * _powerCurve.Evaluate(currentPercentLowRideDistance);
        //// geclampte schrittweite + aktuelle prozentuale position = next percent offset
        //float newPercentLowRideDistance = stepPercentLowRideDistance + currentPercentLowRideDistance;

        
        //this.transform.position = StartingPos + (this.transform.up * _maxDistance * newPercentLowRideDistance);
        #endregion

        // NEW: offset wheel collider spring target position

        // 1. Prozent der aktuellen Suspension spring target position [0,1]
        float curSpringPos = wheelCollider.suspensionSpring.targetPosition;
        // 2. Prozent der gewünschten Suspension spring target position [0 = komplett ausgefahren, 1 = eingefahren]
        float goalSpringPos = (1f - minGroundDistance)  - _strength.Remap(0, 1f, 0, 1f-minGroundDistance);
        // 3. geclampter schritt * _powerCurve multiplicator(based on currentPercent)
        float springStep = Mathf.Clamp(goalSpringPos - curSpringPos, -_stepSize, _stepSize) * _powerCurve.Evaluate(curSpringPos);
        // 4. new Spring position
        float newSpringPos = curSpringPos + springStep;
        // Setze wheelCollider spring target pos (unnötiger kack aber das ist einfach programmierung)
        JointSpring newSpring = wheelCollider.suspensionSpring;
        newSpring.targetPosition = newSpringPos;

        wheelCollider.suspensionSpring = newSpring;
    }

    public void OffsetWheel(float _maxDistance , float _strength) //immediately Sets the wheel position downwards by a percentage
    {
        // rad wird um seine startsposition +  eine maximaltiefe nach unten mit einer stärke verschoben.
        this.wheelCollider.transform.position = StartingPos + (this.transform.up * _maxDistance * _strength);
    }

}
