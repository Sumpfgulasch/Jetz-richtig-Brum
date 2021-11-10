using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;

// MACHT : Extend Wheels, 

[RequireComponent (typeof(Rigidbody))]
public class MagnetBehavior : CarBehavior
{
    const string R = "References";
    const string S = "Settings";
    const string I = "Input";

    [TitleGroup(S)] public int magnetPowerAcceleration = 40;
    [TitleGroup(S)] public int magnetPowerMaxVelocity = 100;

    [TitleGroup(S)] [MinMaxSlider(0f, 2.5f, true)] public Vector2 minMaxGroundDistance = new Vector2(0.1f, 1f);// The minimum/maximum length that the wheels can extend - minimum = x component || maximum = y component
    [TitleGroup(R)] private Vector2 curMinMaxGroundDistance = new Vector2();

    [TitleGroup(S)] [Tooltip("Brake when magnetPower is active and the player doesn't accalerate")] public bool magnetPowerAutoBrake = true;
    [TitleGroup(S)] [Range(0, 1f), ShowIf("magnetPowerAutoBrake")] public float magnetPowerBrakeFactor = 0.98f;
    [TitleGroup(S)] public AnimationCurve magnetPowerDistanceCurve = AnimationCurve.EaseInOut(0f,1f,6f,0f);
    [TitleGroup(S)] public bool limitMagnetTime = true;
    [TitleGroup(S)] [ShowIf("limitMagnetTime")] public float magnetMaxTime = 8f;
    [TitleGroup(S)] [ShowIf("limitMagnetTime")] public float magnetRefillFactor = 4f;
    [TitleGroup(R)] private float magnetTimer = 0;


    [TitleGroup(I)] private bool wheelsOut = false;
    [TitleGroup(I)] private bool magnetIsActive = false;
    [TitleGroup(I)] [ShowInInspector] public bool MagnetIsActive { get => magnetIsActive; set {magnetIsActive = value; SetMagnetVisualisation(value); } } // always set the visualisation when the magnet activation is set.


    [TitleGroup(I)] private LowRideActivity lowRideActivity = new LowRideActivity();
    [TitleGroup(S)] public AnimationCurve lowRideActivityMagnetCurve = AnimationCurve.Linear(0f,1f,1f,0f);
    [TitleGroup(S)] public AnimationCurve lowRideActivityAlignCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);


    [TitleGroup(S)] [Range(0f, 2.5f)] public float extendedWheelsLowRideDistance = 1.2f;
    [TitleGroup(S)] [MinMaxSlider(0f, 1f, true)] public Vector2 extendWheelsTime = new Vector2(0, 0.2f);
    [TitleGroup(R)] private List<Coroutine> extendWheelsRoutines = new List<Coroutine>();


    [TitleGroup(R)] public Material wheels_defaultMat;
    [TitleGroup(R)] public Material wheels_magnetPowerMat;
    [TitleGroup(R)] public MeshRenderer[] wheelMeshes;
    [TitleGroup(R)] public Vector3[] magnetForcePositions = new Vector3[4] { new Vector3(0,-0.22f,-1.839f), new Vector3(0f,0f,0f), new Vector3(0f,-0.22f,1.926f), new Vector3(0f,0f,0f) };
    [TitleGroup(R)] private Rigidbody rB;
    [TitleGroup(R)] public Image magnetUI;


    [TitleGroup(I)] private bool hasLowRideBehavior = false;
    [TitleGroup(R)] private LowRideBehavior lowRideBehavior = null;

    [TitleGroup(I)] private bool hasThrustBehavior = false;
    [TitleGroup(R)] private ThrustBehavior thrustBehavior = null;

    [TitleGroup(I)] private bool hasAutoAlignBehavior = false;
    [TitleGroup(R)] private AutoAlignBehavior autoAlignBehavior = null;

    //------------------------ SETUP
    public override bool SetRequirements()
    {
        //Set magnetUI - TODO, create one or take the fallback
        if (magnetUI = null)
        {
            Debug.Log("magnet UI is missing");
        }


        //SetMaterials
        if (wheels_defaultMat == null){ wheels_defaultMat = SceneObjectManager.Instance.WheelDefaultMaterial; }
        if (wheels_magnetPowerMat == null){ wheels_magnetPowerMat = SceneObjectManager.Instance.WheelMagnetMaterial; }

        //Get the Wheel Meshes
        wheelMeshes = new MeshRenderer[4];
        wheelMeshes[0] = cC.frontWheelRMesh != null ? cC.frontWheelRMesh : null;
        wheelMeshes[1] = cC.frontWheelLMesh != null ? cC.frontWheelLMesh : null;
        wheelMeshes[2] = cC.backWheelRMesh != null ? cC.backWheelRMesh : null;
        wheelMeshes[3] = cC.backWheelLMesh != null ? cC.backWheelLMesh : null;


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
        }

        //Sets an anchor for the minMaxDistances (BUT I DONT UNDERSTAND :D) - refactor?
        curMinMaxGroundDistance = minMaxGroundDistance;

        //CHECK IF INITIALISATION WAS SUCCESSFULL

        if (hasThrustBehavior == true && thrustBehavior == null) { return false; } // wenn bei der initialisierung des bools etwas schief gegangen ist. - return false, also sage dass die initialisierung schief ging.
        if (hasLowRideBehavior == true && lowRideBehavior == null) { return false; } // wenn bei der initialisierung des bools etwas schief gegangen ist. - return false, also sage dass die initialisierung schief ging.
        if (hasAutoAlignBehavior == true && autoAlignBehavior == null) { return false; } // wenn bei der initialisierung des bools etwas schief gegangen ist. - return false, also sage dass die initialisierung schief ging.

        if (rB == null) // check all components
        {
            return false;
        }


        if (wheelMeshes.Contains(null)) // check the wheelMeshes
        {
            Debug.LogWarning(this.transform.name + ": MagnetBehavior cant be executed Properly.");
            return false;
        }
 

        return true;
    }

    //------------------------ INPUT HANDLING
    public void OnMagnetPowerToggle(InputValue inputValue)
    {
        Debug.Log("Toggled");
        if (EnabledBehavior)
        {
            // Button mode #1: De- & activate
            if (inputValue.isPressed)
            {
                if (MagnetIsActive)
                {
                    MagnetIsActive = false;
                }
                else
                {
                    MagnetIsActive = true;
                }
            }
        }
    }

    public void OnMagnetPowerPress(InputValue inputValue)
    {
        Debug.Log("Pressed");
        if (EnabledBehavior)
        {
            // Button mode #2: Pressed
            if (inputValue.isPressed)
            {
                MagnetIsActive = true;
            }
            else
            {
                MagnetIsActive = false;
            }
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
                    if (MagnetIsActive && thrustBehavior.ThrustInputVal == 0 && cC.drivingStateInfo != DrivingState.InAir)
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
                wheel.material = wheels_defaultMat;
            }
            else
            {
                wheel.material = wheels_magnetPowerMat;
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
            float alignStrength = Mathf.Clamp01(lowRideActivityAlignCurve.Evaluate(lowRideActivity.HighestValue));     // wenn lowRide-Activity: kein autoAlign
            autoAlignBehavior.AutoAlignCar(DrivingState.InAir, alignStrength);
        }
        AddPullForce(lowRideActivity.Values);
    }
    private void ManageMagnetTimeLimit()
    {
        // UI
        if (magnetIsActive)
        {
            magnetTimer = Mathf.Clamp(magnetTimer + Time.deltaTime, 0, magnetMaxTime);
        }
        else
        { 
            magnetTimer = Mathf.Clamp(magnetTimer - magnetRefillFactor * Time.deltaTime, 0, magnetMaxTime);
        }

        if (magnetUI != null)
        {
            magnetUI.fillAmount = 1f - magnetTimer / magnetMaxTime;
            magnetUI.color = Color.Lerp(Color.red, Color.green, magnetUI.fillAmount);
        }

        // Deactivate magnet
        if (magnetTimer == magnetMaxTime)
        {
            MagnetIsActive = false;
            ToggleExtendedGroundDistance(false);
        }
    }

    /// <summary>
    /// Scheiß funktion. Nochmal schön schreiben. Fügt dem Auto Force nach unten hinzu, abhängig von der Distanz der targetSurface.
    /// </summary>
    /// <param name="strengths">front, right, back, left. [0,1]</param>
    private void AddPullForce(float[] strengths)
    {
        float surfaceDistance = 1000;

        // 1. Get vector pointing downwards from car
        Vector3 downVector = -transform.up;

        // 2. Get distance factor
        RaycastHit hit;                                                                     // scheiße mit extra raycast, geschieht schon in autoAlignment, aber eben nicht jeden frame...
        if (Physics.Raycast(transform.position, -this.transform.up, out hit))
        {
            surfaceDistance = (hit.point - transform.position).magnitude;
        }
        float distanceFactor = Mathf.Clamp01(magnetPowerDistanceCurve.Evaluate(surfaceDistance));

        // 3. Add force
        Vector3 force = downVector * magnetPowerAcceleration * distanceFactor;
        //rB.AddForce(force, ForceMode.Acceleration);
        float frontStrength = Mathf.Clamp01(lowRideActivityMagnetCurve.Evaluate(strengths[0]));
        float backStrength = Mathf.Clamp01(lowRideActivityMagnetCurve.Evaluate(strengths[2]));
        rB.AddForceAtPosition(force * 0.5f * frontStrength, magnetForcePositions[0], ForceMode.Acceleration);          // front wheels
        rB.AddForceAtPosition(force * 0.5f * backStrength, magnetForcePositions[2], ForceMode.Acceleration);          // back wheels

        // 4. Max speed
        rB.velocity = rB.velocity.normalized * Mathf.Clamp(rB.velocity.magnitude, 0, magnetPowerMaxVelocity);
    }

    /// <summary>
    /// Whenn using magnet+extendWheels, the curMinMaxGroundDistance-variable (which is used for lowRide) shall be overwritten.
    /// </summary>
    /// <param name="value"></param>
    public void ToggleExtendedGroundDistance(bool value)
    {
        wheelsOut = value;

        if (value == true)
        {
            // 1. Stop all routines (unnötige lange coroutinenscheiße)
            foreach (Coroutine routine in extendWheelsRoutines)
            {
                if (routine != null)
                    StopCoroutine(routine);
            }
            extendWheelsRoutines.Clear();

            // 2. Start new routine
            Vector2 targetMinMaxGroundDistance = minMaxGroundDistance + Vector2.one * extendedWheelsLowRideDistance;
            extendWheelsRoutines.Add(StartCoroutine(ShiftMinMaxGroundDistance(curMinMaxGroundDistance, targetMinMaxGroundDistance, extendWheelsTime.x)));
        }
        else
        {
            // 1. Stop all routines
            foreach (Coroutine routine in extendWheelsRoutines)
            {
                if (routine != null)
                    StopCoroutine(routine);
            }
            extendWheelsRoutines.Clear();

            // 2. Start new routine
            Vector2 targetMinMaxGroundDistance = minMaxGroundDistance;
            extendWheelsRoutines.Add(StartCoroutine(ShiftMinMaxGroundDistance(curMinMaxGroundDistance, targetMinMaxGroundDistance, extendWheelsTime.y)));
        }
    }

    /// <summary>
    /// Lerp the curMinMaxGroundDistance-variable to another value over time.
    /// </summary>
    private IEnumerator ShiftMinMaxGroundDistance(Vector2 startMinMax, Vector2 targetMinMax, float time)
    {
        float timer = 0;
        while (timer < time)
        {
            curMinMaxGroundDistance = Vector2.Lerp(startMinMax, targetMinMax, timer / time);
            timer += Time.deltaTime;

            yield return null;
        }
        curMinMaxGroundDistance = targetMinMax;
    }

}
