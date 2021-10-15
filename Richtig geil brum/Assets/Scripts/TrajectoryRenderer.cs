using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

[RequireComponent(typeof(LineRenderer), typeof(Rigidbody))]

public class TrajectoryRenderer : SerializedMonoBehaviour
{
    const string S = "Settings";
    const string D = "Debug";
    const string R = "References";

    [TitleGroup(S)] public LayerMask rayCastLayerMask = ~ 0; // sets layermask to everything on default ---   this ~  flips the layermask
    [TitleGroup(S)] public float trajectoryVertDist = 0.25f; // Step distance for the trajectory
    [TitleGroup(S)] public float maxCurveLength = 5; // Max length of the trajectory


    [TitleGroup(R)] private Rigidbody rB = null;    // Reference to the attached Rigidbody
    [TitleGroup(R)] private LineRenderer lR = null; //Reference to the line renderer to predict trajectory


    [TitleGroup(D)] private bool showTrajectory = true;
    [TitleGroup(D), OdinSerialize] public bool ShowTrajectory { get { return showTrajectory; } set { showTrajectory = value; if (showTrajectory == false) { ClearTrajectory(); } } }
    [TitleGroup(D)] private bool showHitPoint = false;
    [TitleGroup(D), OdinSerialize] public bool ShowHitPoint { 
        get { return showHitPoint; } 
        set 
        { 
            showHitPoint = value; 
            if (showHitPoint == true) 
            {
                if (hitPointGO == null)
                {
                    hitPointGO = new GameObject("hitPointGameObject");
                } 
            } 
        } 
    }
    [TitleGroup(D)] public GameObject hitPointGO = null;

    [TitleGroup(R), HideInInspector] public Trajectory trajectory;



    private void Start()
    {
        Debug.Log("Dont forget to set your RayCastLayerMask");

        lR = this.gameObject.GetComponent<LineRenderer>();
        if (lR == null)
        {
            lR = this.gameObject.AddComponent<LineRenderer>();
            Debug.LogWarning("TrajectoryRenderer on " + gameObject.name + ", Added Linerenderer");
        }

        rB = this.gameObject.GetComponent<Rigidbody>();
        if (rB == null)
        {
            rB = this.gameObject.AddComponent<Rigidbody>();
            Debug.LogWarning("TrajectoryRenderer on " + gameObject.name + ", Added Rigidbody");
        }

        ShowHitPoint = true;
    }
    private void FixedUpdate()
    {
        if(ShowTrajectory) // falls wir das auf mehrere objekte drauftun, sollte der toggle auf dem trajectoryrenderer liegen :), aber die idee die du damit eingebaut hast ist nun dort: Zeile 50 im carController
        {
            trajectory = GetTrajectory(this.transform.position, rB.velocity, trajectoryVertDist, maxCurveLength);
            DrawTrajectory(trajectory);
            if (ShowHitPoint)
            {
                if (hitPointGO != null)
                {
                    if (trajectory.HasHit)
                    {
                        hitPointGO.transform.position = trajectory.HitPoint;
                        hitPointGO.transform.rotation = Quaternion.LookRotation(trajectory.HitNormal);
                    }
                }
                else
                {
                    Debug.Log("hitPointGo ist null, ACHTUNG");
                }
            }
        }
    }

    public Trajectory GetTrajectory(Vector3 _initialPosition, Vector3 _initialVelocity, float _trajectoryVertDist = 0.25f, float _maxCurveLength = 5f)
    {
        if (_initialVelocity.magnitude > 0f) // soll nur berechnet werden wenn es eine velocity groesser als 0 gibt.
        {
            Trajectory trajectory = null; // initialize trajectory Class

            List<Vector3> curvePoints = new List<Vector3>();  // Create a list of trajectory points
            curvePoints.Add(_initialPosition);

            Vector3 currentPosition = _initialPosition; // Initial values for trajectory
            Vector3 currentVelocity = _initialVelocity;

            RaycastHit hit;
            Ray ray = new Ray(currentPosition, currentVelocity.normalized);

            int counter = 0;
            // Die while schleife kann alles zum abstuerzen bringen... hee hee... ja -  kacke, aber koennen wir later fixen.
            while (!Physics.Raycast(ray, out hit, trajectoryVertDist, rayCastLayerMask) && Vector3.Distance(_initialPosition, currentPosition) < maxCurveLength)  // Loop until hit something or distance is too great
            {
                float t = trajectoryVertDist / currentVelocity.magnitude;  // Time to travel distance of trajectoryVertDist

                currentVelocity = currentVelocity + t * Physics.gravity;   // Update position and velocity
                currentPosition = currentPosition + t * currentVelocity;

                curvePoints.Add(currentPosition);   // Add point to the trajectory

                ray = new Ray(currentPosition, currentVelocity.normalized); // Create new ray

                // hot fix gegen Abstürze
                counter++;
                if (counter >= 200)
                {
                    Debug.Log("STOP TRAJECTORY; mehr als 200 raycasts.");
                    break;
                }
            }


            if (hit.transform)  // If something was hit, add last point there
            {
                curvePoints.Add(hit.point);
                trajectory = new Trajectory(curvePoints.ToArray(), true, hit.transform.gameObject, hit.normal);
            }
            else
            {
                trajectory = new Trajectory(curvePoints.ToArray(), false, null, Vector3.zero);
            }

            return trajectory;
        }
        else
        {
            Debug.Log("Velocity.magnitude is 0, cant calculate a trajectory");
            return new Trajectory(new Vector3[0], false);       // macht die Abfragen in anderen Skripten leichter; sorry wenn das programmier-technisch nicht sauber ist :p
            //return null;
        }
    }

    private void DrawTrajectory(Vector3 _initialPosition, Vector3 _initialVelocity, float _trajectoryVertDist = 0.25f, float _maxCurveLength = 5f)
    {
        Trajectory trajectory = GetTrajectory(_initialPosition, _initialVelocity, _trajectoryVertDist, _maxCurveLength);
        lR.positionCount = trajectory.Positions.Length;
        lR.SetPositions(trajectory.Positions);
    }
    private void DrawTrajectory(Trajectory _trajectory)
    {
        if (_trajectory != null)
        {
            lR.positionCount = _trajectory.Positions.Length;
            lR.SetPositions(_trajectory.Positions);
        }
        else
        {
            Debug.Log("Trajectory is null");
        }

    }


    public void ClearTrajectory() // Clears LinePositions
    {
        if (lR != null)
        {
            lR.positionCount = 0;
        }
        else
        {
            Debug.Log("LineRenderer is null, please make sure it isnt null");
        }
    }
}
