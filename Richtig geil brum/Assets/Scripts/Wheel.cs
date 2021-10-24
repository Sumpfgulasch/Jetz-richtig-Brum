using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.VFX;

public class Wheel : SerializedMonoBehaviour
{
    const string R = "References";
    const string S = "Settings";
    const string H = "Helper";


    [TitleGroup(R)] CarController carController;
    [TitleGroup(R)] public WheelCollider wheelCollider;
    [TitleGroup(R)] public Transform wheelModelTransform; // visual wheel model
    [TitleGroup(R)] public VisualEffect wheelSmokeVisualEffect; // smoke effect for this wheel


    [TitleGroup(S), GUIColor(0.5f, 0f, 0f)] [MinMaxSlider(0f,10f)] public Vector2 sidewaysStiffnessMinMax = new Vector2(1f,10f);

    [TitleGroup(S)] [Range(0f, 10000f)] public float wheelFrictionSmokeActivationMaximum = 5000f; // sorry fuer die bennenung : is an arbitrary METRIC to remap non realistic friction forces into a 0 to 1 range
    [TitleGroup(S)] [Range(0f, 1f)] public float wheelFrictionSmokeActivationThreshold01 = 0.5f; // sorry fuer die bennenung : is an arbitrary unit which determines when the smoke should be generated
    [TitleGroup(S)] [MinMaxSlider(0f,50f)] public Vector2 wheelFrictionSmokeParticleAmountRemap = new Vector2(3f,20f); // sorry fuer die bennenung : remaps the "wheelFrictionSmokeActivationMaximum  to 1" friction to the particle Spawn Rate


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

        //Set SmokeVisualEffect
        if (wheelSmokeVisualEffect == null) //wenn der smokevisualEffect == null ist
        {
            wheelSmokeVisualEffect = this.transform.GetComponentInChildren<VisualEffect>(); // dann suche ihn in den childs.
            if(wheelSmokeVisualEffect == null) // wenn er immernoch null ist
            {
                if (SceneObjectManager.Instance.wheelSmokeVisualEffectAsset != null) // wenn das Visual Effekt Asset "wheelSmoke" in sceneObject Manager gesetzt wurde
                {
                    GameObject gO = new GameObject("wheelSmokeVisualEffect"); // fuege neues Gameobject hinzu
                    gO.transform.position = this.transform.position - new Vector3(0f, wheelCollider.radius, 0f) ; // setze die Position des Game Objects
                    gO.transform.parent = this.transform; // setze dieses Objekt als Parent
                    wheelSmokeVisualEffect = gO.AddComponent<VisualEffect>(); // Fuege Visual Effect hinzu.
                    wheelSmokeVisualEffect.visualEffectAsset = SceneObjectManager.Instance.wheelSmokeVisualEffectAsset; //setze das Visual Effect Asset aus dem sceneObjectManager.
                    wheelSmokeVisualEffect.SetFloat("SpawnrateConstant", 0f); // initialisiere die Spawnrate vom smoke auf 0
                }
                else
                {
                    Debug.LogWarning("wheelSmokeVisualEffectAsset wurde im SceneObjectManager nicht gesetzt, dat ist nicht so gut.");
                }
            }
        }

    }

    void Update() 
    {
        if (this.transform.hasChanged) // changes the Model when the collider changes
        {
            UpdateWheelPose();
            transform.hasChanged = false;
        }
        AdjustStiffnessBasedOnSpeed();
        ProduceSmoke();
    }

    private void AdjustStiffnessBasedOnSpeed() // can be used to prohibit Drifting
    {
        //Todo.
    }
    public void ProduceSmoke()
    {
        if(wheelSmokeVisualEffect != null)
        {
            if (wheelCollider.isGrounded) // wenn das rad aufm boden ist
            {
                this.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion quat);
                wheelSmokeVisualEffect.transform.position = pos - (carController.transform.rotation * new Vector3(0f, wheelCollider.radius, 0f)); // setze die Position vom Rad  zur schnittstelle mit dem boden.

                // eine rein ausgedachte berechnung die nichts mit der realen welt zu tun hat, denke ich
                // vergleiche 2 richtungen die, in die velocity uns draengt und die in die das rad zeigt wenn es 90° zur front zeigen wuerde-> range -1 bis 1
                // falte die negativen werte auf die positiven (durch die ABS operation)
                // Multipliziere die Friction mit der geschwindigkeits starke des autos // and scale it down so the values are nicer to work with.
                float wheelFriction = Mathf.Abs(Vector3.Dot(carController.RB.velocity.normalized, this.transform.right))* carController.RB.velocity.magnitude * 1000f;
                float wheelFriction01 = Mathf.Clamp01(Mathf.InverseLerp(0f,wheelFrictionSmokeActivationMaximum, wheelFriction)); // random gepickte obergrenze, und clamp01 damit die werte kontrollierbar bleiben.

                if (wheelFriction01 > wheelFrictionSmokeActivationThreshold01) // set smoke when Threshold is broken
                {
                    //Debug.Log("Smoke is activated");
                    float power = Mathf.Lerp(wheelFrictionSmokeParticleAmountRemap.x, wheelFrictionSmokeParticleAmountRemap.y, wheelFriction01);
                    wheelSmokeVisualEffect.SetFloat("SpawnrateConstant", power);
                }
                else // reset Smoke effect
                {
                    wheelSmokeVisualEffect.SetFloat("SpawnrateConstant", 0f);
                }
            }
            else
            {
                wheelSmokeVisualEffect.SetFloat("SpawnrateConstant", 0f);
            }


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
