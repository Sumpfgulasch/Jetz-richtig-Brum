using System;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;
using Sirenix.Utilities;


[RequireComponent(typeof(Rigidbody))]
public class MagnetBehavior : CarBehavior
{
    const string R = "References";
    const string S = "Settings";
    const string I = "Input";
    const string H = "Helper";

    [TitleGroup(S)] public int magnetPowerAcceleration = 40;
    [TitleGroup(S)] public int magnetPowerMaxVelocity = 100;

    [TitleGroup(S)] [MinMaxSlider(0f, 2.55f, true)]
    public Vector2
        extendWheelsDistanceOnWheelsOut =
            new Vector2(1.35f,
                2.55f); // The minimum/maximum length that the wheels can extend - minimum = x component || maximum = y component

    [TitleGroup(S)] [Tooltip("Brake when magnetPower is active and the player doesn't accalerate")]
    public bool magnetPowerAutoBrake = true;

    [TitleGroup(S)] [Range(0, 1f), ShowIf("magnetPowerAutoBrake")]
    public float magnetPowerBrakeFactor = 0.98f;

    [TitleGroup(S)] public AnimationCurve magnetPowerDistanceCurve = AnimationCurve.EaseInOut(0f, 1f, 6f, 0f);
    [TitleGroup(S)] public bool limitMagnetTime = true;

    [TitleGroup(S)] [ShowIf("limitMagnetTime")]
    public float magnetMaxTime = 8f;

    [TitleGroup(S)] [ShowIf("limitMagnetTime")]
    public float magnetRefillFactor = 4f;

    [TitleGroup(H)] [ReadOnly] private float magnetTimer = 0;


    [TitleGroup(H)] [ReadOnly] private bool wheelsOut = false;
    [TitleGroup(I)] private bool magnetIsActive = false;

    [TitleGroup(I)] [ShowInInspector]
    public bool MagnetIsActive // Let everything that is dependent on Magnet is Active be run by the Setter (e.g. Visualisation, Toogle Wheel Distance, etc..)
    {
        get => magnetIsActive;
        set
        {
            magnetIsActive = value;
            SetMagnetVisualisation(value);
            ToggleExtendedGroundDistance(value);
        }
    }


    [TitleGroup(S)] public AnimationCurve lowRideActivityMagnetCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [TitleGroup(S)] public AnimationCurve lowRideActivityAlignCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);


    [TitleGroup(R)] public Material wheels_defaultMat;
    [TitleGroup(R)] public Material wheels_magnetPowerMat;
    [TitleGroup(R)] public MeshRenderer[] wheelMeshes;
    [TitleGroup(R)] private Wheel frontWheelR, frontWheelL, backWheelR, backWheelL;

    [TitleGroup(R)] private Wheel[] wheels
    {
        get
        {
            return new[]
            {
                frontWheelR,
                frontWheelL,
                backWheelR,
                backWheelL
            };
        }
    }

    [TitleGroup(R)] public Vector3[] magnetForcePositions =
    {
        new Vector3(0, -0.22f, -1.839f),
        new Vector3(0f, -0.22f, 1.926f)
    };

    [TitleGroup(R)] private Rigidbody rB;
    [TitleGroup(R)] public Image magnetUI;


    [TitleGroup(I)] private bool hasLowRideBehavior;
    [TitleGroup(R)] private LowRideBehavior lowRideBehavior;

    [TitleGroup(I)] private bool hasThrustBehavior = false;
    [TitleGroup(R)] private ThrustBehavior thrustBehavior = null;

    [TitleGroup(I)] private bool hasAutoAlignBehavior = false;
    [TitleGroup(R)] private AutoAlignBehavior autoAlignBehavior = null;

    //------------------------ SETUP
    public override bool SetRequirements()
    {
        //Set magnetUI - TODO, create one or take the fallback
        if (magnetUI == null)
        {
            Debug.Log("magnet UI is missing");
        }

        //SetMaterials
        if (wheels_defaultMat == null)
        {
            wheels_defaultMat = SceneObjectManager.Instance.WheelDefaultMaterial;
        }

        if (wheels_magnetPowerMat == null)
        {
            wheels_magnetPowerMat = SceneObjectManager.Instance.WheelMagnetMaterial;
        }

        //Get the Wheel Meshes
        wheelMeshes = new MeshRenderer[4];
        wheelMeshes[0] = cC.frontWheelRMesh != null ? cC.frontWheelRMesh : null;
        wheelMeshes[1] = cC.frontWheelLMesh != null ? cC.frontWheelLMesh : null;
        wheelMeshes[2] = cC.backWheelRMesh != null ? cC.backWheelRMesh : null;
        wheelMeshes[3] = cC.backWheelLMesh != null ? cC.backWheelLMesh : null;

        //SETUP THE WHEELS
        frontWheelR = cC.frontWheelR != null ? cC.frontWheelR : null;
        frontWheelL = cC.frontWheelL != null ? cC.frontWheelL : null;
        backWheelR = cC.backWheelR != null ? cC.backWheelR : null;
        backWheelL = cC.backWheelL != null ? cC.backWheelL : null;


        //Set ThrustBehavior (if it has it)
        if (cC.HasBehavior<ThrustBehavior>())
        {
            thrustBehavior = cC.GetBehavior<ThrustBehavior>();
            hasThrustBehavior = true;
        }

        //Set LowRideBehavior (if it has it)
        if (cC.HasBehavior<LowRideBehavior>())
        {
            lowRideBehavior = cC.GetBehavior<LowRideBehavior>();
            hasLowRideBehavior = true;
        }

        //Set AutoAlignBehavior (if it has it)
        if (cC.HasBehavior<AutoAlignBehavior>())
        {
            autoAlignBehavior = cC.GetBehavior<AutoAlignBehavior>();
            hasAutoAlignBehavior = true;
        }

        // SET THE RIGIDBODY.
        rB = cC.gameObject.GetComponent<Rigidbody>();
        if (rB == null)
        {
            rB = cC.gameObject.AddComponent<Rigidbody>();
            Debug.LogWarning("Ich habe einen Rigidbody hinzugefuegt, achtung, das darf auf keinen fall passieren. ");
        }

        //CHECK IF INITIALISATION WAS SUCCESSFULL

        if (hasThrustBehavior == true && thrustBehavior == null)
        {
            return false;
        } // wenn bei der initialisierung des bools etwas schief gegangen ist. - return false, also sage dass die initialisierung schief ging.

        if (hasLowRideBehavior == true && lowRideBehavior == null)
        {
            return false;
        } // wenn bei der initialisierung des bools etwas schief gegangen ist. - return false, also sage dass die initialisierung schief ging.

        if (hasAutoAlignBehavior == true && autoAlignBehavior == null)
        {
            return false;
        } // wenn bei der initialisierung des bools etwas schief gegangen ist. - return false, also sage dass die initialisierung schief ging.

        if (rB == null || wheelMeshes.Contains(null) || wheels.Contains(null)) // check all components
        {
            return false;
        }

        return true;
    }

    //------------------------ INPUT HANDLING
    public void OnMagnetPowerToggle(InputValue inputValue)
    {
        Debug.Log("Toggled");
        if (!EnabledBehavior || !inputValue.isPressed)
            return;

        MagnetIsActive = !MagnetIsActive;
    }

    public void OnMagnetPowerPress(InputValue inputValue)
    {
        Debug.Log("Pressed");
        if (EnabledBehavior)
        {
            // Button mode #2: Pressed
            MagnetIsActive = inputValue.isPressed;
        }
    }

    //------------------------ BEHAVIOR

    public override void ExecuteBehavior(Func<bool> _shouldExecute)
    {
        if (MagnetIsActive)
        {
            MagnetPower(); //pull und align - bool - spielerinput
        }

        if (limitMagnetTime)
            ManageMagnetTimeLimit(); //limit magnettime, wenn magnetpower - bool

        // MagnetPower automatic brake
        if (magnetPowerAutoBrake)
        {
            if (cC.drivingStateInfo != DrivingState.InAir) // wenn auto in der luft ist
            {
                if (hasThrustBehavior) //wenn ein thrust behavior da ist, brake nur, wenn es auch keinen input gibt
                {
                    if (MagnetIsActive && thrustBehavior.ThrustInputVal == 0 &&
                        cC.drivingStateInfo != DrivingState.InAir)
                    {
                        rB.BrakeVelocity(magnetPowerBrakeFactor);
                    }
                }
                else //wenn KEIN thrust behavior da ist, breake nur wenn der magnet aktiv ist.
                {
                    if (MagnetIsActive)
                    {
                        rB.BrakeVelocity(magnetPowerBrakeFactor);
                    }
                }
            }
        }
    }

    private void SetMagnetVisualisation(bool _active)
    {
        //Wheels
        foreach (MeshRenderer wheel in wheelMeshes)
        {
            if (_active)
            {
                wheel.material = wheels_magnetPowerMat;
            }
            else
            {
                wheel.material = wheels_defaultMat;
            }
        }
    }

    /// <summary>
    /// Align car to a surface and add a force of the car to that surface.
    /// </summary>
    /// <returns></returns>
    private void MagnetPower()
    {
        if (hasAutoAlignBehavior) //  fuehre AutoAlign nur aus, wenn es ein Autoalignbehavior gibt.
        {
            if (hasLowRideBehavior) // wenn lowRide-Activity: kein autoAlign
            {
                var alignStrength =
                    Mathf.Clamp01(lowRideActivityAlignCurve.Evaluate(lowRideBehavior.LowRideActivity.HighestValue));
                autoAlignBehavior.AutoAlignCar(alignStrength); // TODO - darf hier nicht aufgerufen werden.
            }
            else // wenn kein lowRideBehavior, immer voll autoalign
            {
                autoAlignBehavior.AutoAlignCar();
            }
        }

        if (hasLowRideBehavior) // wenn lowRide-Activity: add pull force mit dem lowRideActivity in mind.
        {
            AddPullForce(rB, magnetForcePositions, magnetPowerDistanceCurve, magnetPowerAcceleration,
                lowRideActivityMagnetCurve, magnetPowerMaxVelocity, lowRideBehavior.LowRideActivity);
        }
        else // lowRideActivityValues  = 0f,0f,0f,0f
        {
            AddPullForce(rB, magnetForcePositions, magnetPowerDistanceCurve, magnetPowerAcceleration,
                lowRideActivityMagnetCurve, magnetPowerMaxVelocity, new LowRideActivity());
        }
    }

    private void ManageMagnetTimeLimit()
    {
        // UI
        if (MagnetIsActive)
        {
            magnetTimer = Mathf.Clamp(magnetTimer + Time.deltaTime, 0, magnetMaxTime);
        }
        else
        {
            magnetTimer = Mathf.Clamp(magnetTimer - magnetRefillFactor * Time.deltaTime, 0, magnetMaxTime);
        }

        // Draw UI
        if (magnetUI != null) //if it isnt null
        {
            magnetUI.fillAmount = 1f - magnetTimer / magnetMaxTime;
            magnetUI.color = Color.Lerp(Color.red, Color.green, magnetUI.fillAmount);
        }

        // Deactivate magnet
        if (magnetTimer == magnetMaxTime)
        {
            MagnetIsActive = false;
        }
    }

    /// <summary>
    /// Scheiß funktion. Nochmal schön schreiben. Fügt dem Auto Force nach unten hinzu, abhängig von der Distanz der targetSurface.
    /// </summary>
    /// <param name="_lowRideActivityValues">front, right, back, left. [0,1]</param>
    private void AddPullForce(Rigidbody _rB, Vector3[] _magnetForcePositions, AnimationCurve _magnetPowerDistanceCurve,
        float _magnetPowerAcceleration, AnimationCurve _lowRideActivityMagnetCurve, float _magnetPowerMaxVelocity,
        LowRideActivity _lowRideActivity)
    {
        if (_magnetForcePositions.Length != 2)
        {
            Debug.LogWarning("MagnetForcePositions Array doesnt contain 2 Positions");
            return;
        }

        float surfaceDistance = 1000;

        // 1. Get vector pointing downwards from car
        Vector3 downVector = -this.transform.up;

        // 2. Get distance factor
        RaycastHit hit; // scheiße mit extra raycast, geschieht schon in autoAlignment, aber eben nicht jeden frame...
        if (Physics.Raycast(this.transform.position, downVector, out hit))
        {
            surfaceDistance = (hit.point - this.transform.position).magnitude;
        }

        float distanceFactor = Mathf.Clamp01(_magnetPowerDistanceCurve.Evaluate(surfaceDistance));

        // 3. Add force
        Vector3
            force = downVector * _magnetPowerAcceleration *
                    distanceFactor; // Q: warum wird forcedistance vom Mittelpunkt des autos berechnet, aber die force an den achsen applied?
        float frontStrength = Mathf.Clamp01(_lowRideActivityMagnetCurve.Evaluate(_lowRideActivity[CarDir.F]));
        float backStrength = Mathf.Clamp01(_lowRideActivityMagnetCurve.Evaluate(_lowRideActivity[CarDir.B]));
        _rB.AddForceAtPosition(force * 0.5f * frontStrength, this.transform.TransformPoint(_magnetForcePositions[0]),
            ForceMode.Acceleration); // front wheels
        _rB.AddForceAtPosition(force * 0.5f * backStrength, this.transform.TransformPoint(_magnetForcePositions[1]),
            ForceMode.Acceleration); // back wheels

        Debug.DrawRay(this.transform.TransformPoint(_magnetForcePositions[1]), -this.transform.up * frontStrength,
            Color.red);
        Debug.DrawRay(this.transform.TransformPoint(_magnetForcePositions[0]), -this.transform.up * backStrength,
            Color.red);

        // 4. Max speed
        _rB.velocity = _rB.velocity.normalized * Mathf.Clamp(_rB.velocity.magnitude, 0, _magnetPowerMaxVelocity);
    }

    ///// <summary>
    ///// Sets the TargetExtension Distance in Wheels. The Blend Timing is done by the wheels.
    ///// </summary>
    ///// <param name="_enabled"></param>
    private void ToggleExtendedGroundDistance(bool value)
    {
        wheelsOut = value;
        wheels.ForEach(wheel => wheel.TargetExtensionDistanceMinMax = wheelsOut
            ? cC.minMaxExtendWheelsDistancesOut
            : cC.minMaxExtendWheelsDistancesIn);
    }
}