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

    }

    void Update() 
    {
        if (this.transform.hasChanged) // changes the Model when the collider changes
        {
            UpdateWheelPose();
            transform.hasChanged = false;
        }
    }


    private void UpdateWheelPose()
    {
        this.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion quat);

        this.wheelModelTransform.position = pos;
        this.wheelModelTransform.rotation = quat;
    }

    public void OffsetWheelGradually(Vector2 _stepSizePlusMinus, float _strength, Vector2 _minMaxGroundDistance, AnimationCurve _powerCurve, WheelOffsetModes _wheelOffsetMode, bool invertedStrength = false) //Pushes the Wheel towards a goal, by an step amount(so it has to be called multiple times to reach its goal)
    {
        switch (_wheelOffsetMode)
        {
            case WheelOffsetModes.SuspensionDistance:
            {
                    // Damit low ride bei JUMP immer noch funzt
                    if (invertedStrength)
                    {
                        _strength = 1f - _strength;
                    }

                // prozent der aktuellen position von startposition bis maximalposition
                float currentPercentLowRideDistance = wheelCollider.suspensionDistance / _minMaxGroundDistance.y;
                // geclampter schritt * _powerCurve multiplicator(based on currentPercent)
                float stepPercentLowRideDistance = Mathf.Clamp
                        (_strength - currentPercentLowRideDistance, 
                        _stepSizePlusMinus.y, 
                        _stepSizePlusMinus.x)
                        * _powerCurve.Evaluate(currentPercentLowRideDistance);
                // geclampte schrittweite + aktuelle prozentuale position = next percent offset
                float newPercentLowRideDistance = wheelCollider.suspensionDistance + stepPercentLowRideDistance;
                wheelCollider.suspensionDistance = Mathf.Clamp(newPercentLowRideDistance, _minMaxGroundDistance.x, _minMaxGroundDistance.y);

                //shitty shit, used that the target position is reseted to 0.5f (muss leider, sonst kann man nicht gut wechseln zwischen den modis)
                JointSpring newSpring = wheelCollider.suspensionSpring;
                newSpring.targetPosition = 0.5f;
                wheelCollider.suspensionSpring = newSpring;

                break;
            }

            case WheelOffsetModes.TargetPosition:
            {
                //NEW: offset wheel collider spring target position

                // 1. Prozent der aktuellen Suspension spring target position [0,1]
                float curSpringPos = wheelCollider.suspensionSpring.targetPosition;
                // 2. Prozent der gewünschten Suspension spring target position [0 = komplett ausgefahren, 1 = eingefahren]
                float goalSpringPos = _strength.Remap(1f, 0f, 0, 1f-_minMaxGroundDistance.x);//(1f - _minGroundDistance)  - _strength.Remap(0, 1f, 0, 1f-_minGroundDistance); 
                // 3. geclampter schritt * _powerCurve multiplicator(based on currentPercent)
                float springStep = Mathf.Clamp(goalSpringPos - curSpringPos, _stepSizePlusMinus.y, _stepSizePlusMinus.x) * _powerCurve.Evaluate(curSpringPos);
                // 4. new Spring position
                float newSpringPos = curSpringPos + springStep;
                // Setze wheelCollider spring target pos (unnötiger kack aber das ist einfach programmierung)
                JointSpring newSpring = wheelCollider.suspensionSpring;
                newSpring.targetPosition = newSpringPos;

                wheelCollider.suspensionSpring = newSpring;

                //shitty shit, used that the suspensionDistance is reseted to maxVal (muss leider, sonst kann man nicht gut wechseln zwischen den modis)
                wheelCollider.suspensionDistance = _minMaxGroundDistance.y;

                break;
            }
        }
    }

}
