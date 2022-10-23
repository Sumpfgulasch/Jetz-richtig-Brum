using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.VFX;
using Sirenix.Serialization;


public class Wheel : SerializedMonoBehaviour
{
    const string R = "References";
    const string EX = "Extension";
    const string P = "Particles";
    const string H = "Helper";
    const string D = "Default";
    const string IN = "Input";

    [TitleGroup(IN)] [SuffixLabel("%", true)] private float targetExtensionPercent = 0f;
    [TitleGroup(IN)] [SuffixLabel("%", true)] [OdinSerialize][ReadOnly] public float TargetExtensionPercent { get => Mathf.Clamp01(targetExtensionPercent); set => targetExtensionPercent = Mathf.Clamp01(value); } // always clamp01 on get and set

    [TitleGroup(EX)] [SuffixLabel("%", true)] private float currentExtensionPercent = 0f;
    [TitleGroup(EX)] [SuffixLabel("%", true)] [OdinSerialize] [ReadOnly] public float CurrentExtensionPercent { get => Mathf.Clamp01(currentExtensionPercent); private set => currentExtensionPercent = Mathf.Clamp01(value); } // always clamp01 on get and set



    [TitleGroup(IN)] [SuffixLabel("Unity Units", true)] private Vector2 targetExtensionDistanceMinMax = new Vector2(0.15f, 1.35f);
    [TitleGroup(IN)] [SuffixLabel("Unity Units",true)] [OdinSerialize] [ReadOnly] public Vector2 TargetExtensionDistanceMinMax { get => targetExtensionDistanceMinMax; set => targetExtensionDistanceMinMax = value; } // always clamp01 on get and set

    [TitleGroup(EX)] [SuffixLabel("Unity Units",true)] private Vector2 currentExtensionDistanceMinMax = new Vector2(0.15f, 1.35f);
    [TitleGroup(EX)] [SuffixLabel("Unity Units",true)] [OdinSerialize] [ReadOnly] public Vector2 CurrentExtensionDistanceMinMax { get => currentExtensionDistanceMinMax; private set => currentExtensionDistanceMinMax = value; } // always clamp01 on get and set



    [TitleGroup(EX)] [Tooltip("Let this never be 0f on y Axis.")] public AnimationCurve extensionStrengthStepMultiplier = AnimationCurve.Linear(0f,1f,1f,1f); // this curve can remap the current Extension float to behave different than linear., should never be 0f on y axis.
    [TitleGroup(EX)] [SuffixLabel("%", true)] public Vector2 extensionStepPercentPlusMinus = new Vector2(0.5f, -0.02f);


    [TitleGroup(P)] [Range(0f, 10000f)] public float wheelFrictionSmokeActivationMaximum = 5000f; // sorry fuer die bennenung : is an arbitrary METRIC to remap non realistic friction forces into a 0 to 1 range
    [TitleGroup(P)] [Range(0f, 1f)] public float wheelFrictionSmokeActivationThreshold01 = 0.5f; // sorry fuer die bennenung : is an arbitrary unit which determines when the smoke should be generated
    [TitleGroup(P)] [MinMaxSlider(0f,50f)] public Vector2 wheelFrictionSmokeParticleAmountRemap = new Vector2(3f,20f); // sorry fuer die bennenung : remaps the "wheelFrictionSmokeActivationMaximum  to 1" friction to the particle Spawn Rate

    [TitleGroup(R)] CarController carController;
    [TitleGroup(R)] public WheelCollider wheelCollider;
    [TitleGroup(R)] public Transform wheelModelTransform; // visual wheel model
    [TitleGroup(R)] public VisualEffect wheelSmokeVisualEffect; // smoke effect for this wheel

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
        //update the wheel Height. and assign the currentExtensionPercent (for Other Scripts and or Debug)
        CurrentExtensionPercent = UpdateWheelHeight(extensionStepPercentPlusMinus, TargetExtensionPercent, extensionStrengthStepMultiplier, currentExtensionDistanceMinMax);
        CurrentExtensionDistanceMinMax = UpdateWheelExtensionMinMax(CurrentExtensionDistanceMinMax, TargetExtensionDistanceMinMax);

        if (this.transform.hasChanged) // changes the Model when the collider changes
        {
            UpdateWheelPose();
            transform.hasChanged = false;
        }

        ProduceSmoke();
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

    private Vector2 UpdateWheelExtensionMinMax(Vector2 _currentExtensionDistanceMinMax, Vector2 _targetExtensionDistanceMinMax)
    {
        if (_currentExtensionDistanceMinMax == _targetExtensionDistanceMinMax) { return _currentExtensionDistanceMinMax; } // wenn beide gleich sind, mach nix.

        Vector2 step = new Vector2(-0.005f * Time.deltaTime * 200f,0.01f * Time.deltaTime * 200f);// can be a variable. but for now, doesnt matter.

        //1. Calculate the difference between the current and the target.
        float diffX = _targetExtensionDistanceMinMax.x- _currentExtensionDistanceMinMax.x;
        float diffY = _targetExtensionDistanceMinMax.y - _currentExtensionDistanceMinMax.y;

        //2. Calculate the maximum step that can be taken this frame.
        float stepX = Mathf.Clamp(diffX, step.x, step.y);
        float stepY = Mathf.Clamp(diffY, step.x, step.y);

        //3. Add the Step to the current.
        return _currentExtensionDistanceMinMax += new Vector2(stepX, stepY);
    }

    /// <summary>
    /// //Pushes the Wheel towards a goal, by an step amount(so it has to be called multiple times to reach its goal) and returns the new extensionPercent.
    /// </summary>
    /// <param name="_extensionStepPercentPlusMinus"></param>
    /// <param name="_targetExtensionPercent"></param>
    /// <param name="_extensionStrengthStepMultiplier"></param>
    /// <param name="_minMaxExtensionDistance"></param>
    private float UpdateWheelHeight(Vector2 _extensionStepPercentPlusMinus, float _targetExtensionPercent, AnimationCurve _extensionStrengthStepMultiplier, Vector2 _minMaxExtensionDistance)
    {
        //1. Get the current Extension % |||| prozent der aktuellen position von startposition bis maximalposition : normalisiere durch - _minMaxGroundDistance.x, dann bekomme das verhaeltnis.
        float currentExtensionPercent = (wheelCollider.suspensionDistance - currentExtensionDistanceMinMax.x) / (currentExtensionDistanceMinMax.y - currentExtensionDistanceMinMax.x);

        //1.5  Wenn target und current gleich sind, mach nix.
        if (currentExtensionPercent == _targetExtensionPercent) { return currentExtensionPercent; }


        //2. Calculate the Step size that is needed to reach the target
        float stepToTarget = _targetExtensionPercent - currentExtensionPercent;

        //3. Get the viable Step(multiplied with the StepMultiplier(which is based on the current ExtensionPercent)) that can be taken this frame |||| geclampter schritt * _powerCurve multiplicator(based on currentPercent)
        float viableStep = Mathf.Clamp
                (stepToTarget,
                _extensionStrengthStepMultiplier.Evaluate(currentExtensionPercent) * _extensionStepPercentPlusMinus.y,
                _extensionStrengthStepMultiplier.Evaluate(currentExtensionPercent) * _extensionStepPercentPlusMinus.x);

        //4. Calculate the stepPercent into Units through the minMaxGroundDistances
        float newExtensionPercent = currentExtensionPercent + viableStep; // calculate the new ExtensionPercent

        //5. assign the new SuspensionDistance
        wheelCollider.suspensionDistance = Mathf.Lerp(_minMaxExtensionDistance.x, _minMaxExtensionDistance.y, newExtensionPercent);

        //shitty shit, used that the target position is reseted to 0.5f (muss leider, sonst kann man nicht gut wechseln zwischen den modis)
        JointSpring newSpring = wheelCollider.suspensionSpring;
        newSpring.targetPosition = 0.5f;
        wheelCollider.suspensionSpring = newSpring;

        return currentExtensionPercent;
    }
}