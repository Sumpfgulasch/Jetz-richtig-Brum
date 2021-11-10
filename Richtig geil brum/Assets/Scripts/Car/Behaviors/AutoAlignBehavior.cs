using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.Serialization;
using Sirenix.OdinInspector;

[RequireComponent(typeof(TrajectoryRenderer), typeof(Rigidbody))]
public class AutoAlignBehavior : CarBehavior
{
    const string R = "References";
    const string S = "Settings";
    const string I = "Input";


    [TitleGroup(S)] public bool stopAutoaligningAfterInAirControl = false;
    [TitleGroup(S)] public bool autoalignCarInAir = false;
    [TitleGroup(S)] private AutoAlignSurface autoAlignSurface = AutoAlignSurface.Trajectory;
    [TitleGroup(S)] [OdinSerialize] public AutoAlignSurface AutoAlignSurface
    {
        get
        {
            return autoAlignSurface;
        }
        set
        {
            autoAlignSurface = value;

            if (trajectoryRenderer != null) // wenn ein trajectoryrenderer assinged ist
            {
                if (value != AutoAlignSurface.Trajectory) // wenn der AutoalignSurface bool umgeschaltet wird und nicht auf Trajectory steht
                {
                    trajectoryRenderer.ShowTrajectory = false; // schalte im trajectory das updaten aus
                    trajectoryRenderer.ClearTrajectory(); // loesche den bestehenden Trajectory im linerendeerer
                }

                else if (value == AutoAlignSurface.Trajectory) // wenn der AutoalignSurface bool umgeschaltet wird und auf Trajectory steht
                {
                    trajectoryRenderer.ShowTrajectory = true; // schalte im trajectory das updaten an
                }
            }
        }
    }

    [TitleGroup(S)] public float maxAngularVelocity = 4f;
    [TitleGroup(S)] public int autoAlignTorqueForce = 40;
    [TitleGroup(S)] [Tooltip("Used to control the amount of torque force. x = 1 heißt dass Auto 100% aligned, x = 0 heißt dass Auto 90° gedreht, x = -1 dass 180° gedreht.")] 
    public AnimationCurve autoAlignAngleCurve = AnimationCurve.Linear(0f,1f,1f,0f);
    [TitleGroup(S)] [Tooltip("Used to reduce the angular velocity when the car is aligned")] 
    public AnimationCurve autoAlignBrakeCurve = AnimationCurve.EaseInOut(0f,1f,1f,0.98f);
    [TitleGroup(S)] public AnimationCurve autoAlignDistanceCurve = AnimationCurve.EaseInOut(0f,1f,13f,0f);
    [TitleGroup(S)] private float targetSurfaceDistance;

    [TitleGroup(R)] public TrajectoryRenderer trajectoryRenderer = null;
    [TitleGroup(R)] private Rigidbody rB;

    [TitleGroup(I)] private bool hasLowRideBehavior = false;
    [TitleGroup(R)] private LowRideBehavior lowRideBehavior;


    //------------------------ SETUP
    public override bool SetRequirements()
    {
        //Set the TrajectoryRenderer
        trajectoryRenderer = cC.gameObject.GetComponent<TrajectoryRenderer>();
        if (trajectoryRenderer == null)
        {
            trajectoryRenderer = cC.gameObject.AddComponent<TrajectoryRenderer>();
        }

        if(cC.HasBehavior<LowRideBehavior>())
        {
            hasLowRideBehavior = true;
            lowRideBehavior = cC.GetBehavior<LowRideBehavior>();
        }

        // SET THE RIGIDBODY.
        rB = cC.gameObject.GetComponent<Rigidbody>();
        if (rB == null)
        {
            rB = cC.gameObject.AddComponent<Rigidbody>();
        }

        //CHECK IF INITIALISATION WAS SUCCESSFULL
        if (hasLowRideBehavior == true && lowRideBehavior == null) // dann ist irgendwas beim setup schief gegangen
        {
            return false;
        }

        if (rB == null || trajectoryRenderer == null) //wenn einer der wichtigen Referenzen null ist
        {
            return false;
        }
        else
        {
            return true;
        }

    }

    //------------------------ BEHAVIOR

    public override void ExecuteBehavior(Func<bool> _shouldExecute)
    {
        if (EnabledBehavior)
        { 
            if (autoalignCarInAir)
            {
                AutoAlignCar(cC.drivingStateInfo);
            }
        }
    }

    public void AutoAlignCar(DrivingState _drivingStateInfo, float strength = 1f)
    {
        if (_drivingStateInfo == DrivingState.InAir)    //wenn das auto in der luft ist.
        {
            Vector3 targetNormal = Vector3.up.normalized;
            RaycastHit hit;

            //Bool to decide wheater or not the lowrideinput should be considerd
            bool lowRideNotNullHasInput = false; // Wichtig, kann nur true sein wenn es ein lowRidebehavior gibt und die bedingungen erfuellt sind.
            if (hasLowRideBehavior)
            {
                lowRideNotNullHasInput = lowRideBehavior.LowRideInputVal != Vector2.zero ? true : false; // wenn ein lowrideInputvalue groeser 0, dann true, ansonsten false
            }
            bool UseForwardDirection = cC.drivingStateInfo == DrivingState.InAir || lowRideNotNullHasInput ? true : false;


            // Decide to which surface the car should align
            switch (AutoAlignSurface)
            {
                case AutoAlignSurface.LowerSurface:
                    {

                        if (Physics.Raycast(this.transform.position, -this.transform.up, out hit))
                        {
                            // get the normal of the hit
                            targetNormal = hit.normal;
                            targetSurfaceDistance = (hit.point - transform.position).magnitude;
                        }
                        break;
                    }
                case AutoAlignSurface.ForwardSurface:
                    {
                        // Ggf. noetig: Hit unter Auto
                        if (Physics.Raycast(this.transform.position, -this.transform.up, out hit))
                        {
                            targetNormal = hit.normal;
                            targetSurfaceDistance = (hit.point - transform.position).magnitude;
                        }

                        if (UseForwardDirection)
                        {
                            if (Physics.Raycast(this.transform.position, this.transform.forward, out hit))
                            {
                                // get the normal of the hit
                                targetNormal = hit.normal;
                                targetSurfaceDistance = (hit.point - transform.position).magnitude;
                            }
                        }

                        break;
                    }
                case AutoAlignSurface.Trajectory:
                    {
                        // Ggf. nötig: Hit unter Auto
                        if (Physics.Raycast(this.transform.position, -this.transform.up, out hit))
                        {
                            targetNormal = hit.normal;
                            targetSurfaceDistance = (hit.point - transform.position).magnitude;
                        }

                        if (trajectoryRenderer.trajectory.HasHit)
                        {
                            if (UseForwardDirection)
                            {
                                var trajectoryNormal = trajectoryRenderer.trajectory.HitNormal;
                                var trajectorySurfaceDistance = (trajectoryRenderer.trajectory.HitPoint - transform.position).magnitude;

                                // nur wenn trajectory-fläche näher ist als downward-surface
                                if (trajectorySurfaceDistance < targetSurfaceDistance)
                                {
                                    targetNormal = trajectoryRenderer.trajectory.HitNormal;
                                    targetSurfaceDistance = (trajectoryRenderer.trajectory.HitPoint - transform.position).magnitude;
                                }

                            }
                        }
                        break;
                    }
            }

            // Rotation Calculation: Add Torque (angular velocity) and brake (angular velocity) if needed.
            AddTorqueAndBrake(targetNormal, autoAlignTorqueForce, maxAngularVelocity, targetSurfaceDistance, autoAlignDistanceCurve, autoAlignAngleCurve, autoAlignBrakeCurve, strength);
        }
    }
    /// <summary>
    /// Add a torque and brake if target rotation is achieved. Takes into consideration: distance to the targetSurface, angle difference to the targetSurfaceNormal
    /// </summary>
    private void AddTorqueAndBrake(Vector3 targetNormal, float torqueForce, float maxTorqueSpeed, float targetSurfaceDistance = 0, AnimationCurve torqueDistanceCurve = null, AnimationCurve torqueAngleCurve = null, AnimationCurve brakeDistanceCurve = null, float strengthPercentage = 1f)
    {
        float distanceFactor = Mathf.Clamp01(torqueDistanceCurve.Evaluate(targetSurfaceDistance));                  // Wert zwischen 0 und 1; entscheidet ob torque und brake verrechnet wird
        float angleDotProduct = Vector3.Dot(transform.up, targetNormal);

        // 1. ADD TORQUE
        Vector3 torqueAxis = Vector3.Cross(transform.up, targetNormal).normalized;                                  // Rotations-Achse = Kreuzprodukt
        float torqueAngleFactor = torqueAngleCurve.Evaluate(angleDotProduct);                                       // Faktor je nach Übereinstimmung von Zielnormale und transform.up
        Vector3 torque = torqueAxis * torqueForce * distanceFactor * torqueAngleFactor * strengthPercentage;        // Finale Torque
        rB.AddTorque(torque, ForceMode.Acceleration);

        // 2. BRAKE                            
        float velocityDistanceFactor = brakeDistanceCurve.Evaluate(Mathf.Clamp01(angleDotProduct));                 // Wert wird 1, wenn Distanz zu hoch (-> keine Veränderung), 0 wenn Distanz niedrig
        float brakeFactor = Mathf.Lerp(1f, velocityDistanceFactor, distanceFactor);                                 // Wenn keine Winkeldifferenz, dann angularVel *= 0; wenn Winkeldifferenz >= 90°, dann keine Veränderung
        rB.angularVelocity *= Mathf.Lerp(1f, brakeFactor, strengthPercentage);                                      // Wirke brake Kraft abhängig von strengPercentage

        // 3. Max speed
        rB.ClampAngularVelocity(maxAngularVelocity);
    }
}

public enum AutoAlignSurface
{
    LowerSurface,
    Trajectory,
    ForwardSurface
}
