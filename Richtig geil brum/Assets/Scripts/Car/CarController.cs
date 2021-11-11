using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;


[RequireComponent(typeof(Rigidbody), typeof(PlayerInput))]
public class CarController : SerializedMonoBehaviour
{
    const string G = "General";
    const string R = "References";
    const string CB = "CarBehavior";
    const string H = "Helper";


    //WOANDERS HIN?
    [GUIColor(1f, 0f, 0f)] [TitleGroup(G)] public float maxSpeed = 20f;        // NOT PROPERLY USED; only for audio


    //SOLLTEN HIER BLEIBEN

    [TitleGroup(G)] public float initialSuspensionDistance = 1f;
    [TitleGroup(G)] private Vector3 centerOfMassOffset = new Vector3(0f, 0f, 0f);
    [TitleGroup(G)] [OdinSerialize] public Vector3 CenterOfMassOffset { get { return centerOfMassOffset; } set { centerOfMassOffset = value; SetCenterOfMass(rB, true); } }



    [TitleGroup(R)] public MeshRenderer frontWheelRMesh, frontWheelLMesh, backWheelRMesh, backWheelLMesh;
    [TitleGroup(R)] public MeshRenderer[] WheelMeshes { get { return new MeshRenderer[4] { frontWheelRMesh, frontWheelLMesh, backWheelRMesh, backWheelLMesh }; } }

    [TitleGroup(R)] public Wheel frontWheelR, frontWheelL, backWheelR, backWheelL;
    [TitleGroup(R)] public Wheel[] Wheels { get { return new Wheel[4] { frontWheelR, frontWheelL, backWheelR, backWheelL }; } }

    [TitleGroup(R)] private Rigidbody rB;
    [TitleGroup(R)] public Rigidbody RB { get { return rB; } }

    [TitleGroup(R), ShowInInspector] private float inAirTime = 0f;

    [TitleGroup(R)] [ShowInInspector] public DrivingState drivingStateInfo {
        get
        {
            if (Wheels != null) // wenn es das array gibt.
            {
                if (!Wheels.Contains(null)) // wenn wheels assigned sind
                {
                    foreach (Wheel wheel in Wheels) // gehe durch alle Wheels
                    {
                        WheelHit hit;
                        if (wheel.wheelCollider.GetGroundHit(out hit)) // wenn ein wheel den Boden beruehrt
                        {
                            return DrivingState.Grounded; // gib grounded zurueck
                        }
                    }
                    return DrivingState.InAir; // ansonsten gib InAir zurueck
                }
            }
            return DrivingState.Grounded; // wenn es nix gibt dann gib grounded zurueck
        }
    }


    [TitleGroup(CB)][OdinSerialize] List<CarBehavior> carBehaviors = new List<CarBehavior>();


    [TitleGroup(H)] public bool showDebugHandles = true;


    private void Reset() // is called when this component is assigned somewhere or the "reset" function on the component is activated.
    {
        FindWheels();
        FindWheelMeshes();

        AssignCarBehaviors();
        OrderCarBehaviors();
        AssignExecutionPriorityBySortation();
    }

    private void OnEnable()
    {
        //Get/Add Rigidbody
        rB = this.gameObject.GetComponent<Rigidbody>();
        if (rB == null)
        {
            rB = this.gameObject.AddComponent<Rigidbody>();
        }

        //Initialize Car Behaviors
        AssignCarBehaviors();
        OrderCarBehaviors();
        AssignExecutionPriorityBySortation();

    }

    void Start() 
    {
        InitSuspensionDistance();   
        SetCenterOfMass(rB);
    }

    void FixedUpdate()
    {
        SetAirTime(ref inAirTime); // sets the time the car is in the air. - when no wheel touches the ground.

        foreach (CarBehavior cB in carBehaviors) // fuer jedes Carbehavior
        {
            if (cB.initializedSuccessfully && cB.EnabledBehavior) // wenn es erfolgreich initialisiert wurde
            {
                cB.ExecuteBehavior(()=>true); // fuehre die Executemethode aus. // lambdaexpression that always returns true. here we could implement a SWITCH that checks what type the carBehavior is, and apply a Rule, for different behaviors.
            }
        }
    }

    // ----------------------------------------- Setup -----------------------------------------

    [TitleGroup(CB)][Button]
    public void AssignCarBehaviors()
    {
        carBehaviors = this.gameObject.GetComponents<CarBehavior>().ToList();// find all CarBehaviors on this Object
    }

    [TitleGroup(CB)][Button]
    public void OrderCarBehaviors()
    {
        carBehaviors = carBehaviors.OrderByDescending(x => x.ExecutionPriority).ToList(); // sorts the carbehaviors by priority (hopefully :D)
        carBehaviors = carBehaviors.Reverse<CarBehavior>().ToList();//carBehaviors.Reverse().ToList(); // make accending.
    }

    [TitleGroup(CB)][Button]
    public void AssignExecutionPriorityBySortation()
    {
        for (int i = 0; i < carBehaviors.Count; i++)
        {
            carBehaviors[i].ExecutionPriority = i;
        }
    }

    private void SetCenterOfMass(Rigidbody _rb, bool _setWhileEditor = false)
    {
        if(_rb == null )
        {
            if (_setWhileEditor == false) // quick hack to use this command in serialisation, without losing the ability to get the logwarning if it fails on runtime.
            { 
                Debug.LogWarning("Rigidbody ist null - es gibt keinen auf diesem Auto");
            }
            return;
        }
        _rb.centerOfMass = centerOfMassOffset;
    }

    public void InitSuspensionDistance()
    {
        if(Wheels != null)
        {
            foreach(Wheel wheel in Wheels)
            {
                if(wheel != null)
                {
                    wheel.wheelCollider.suspensionDistance = initialSuspensionDistance; // Set initial Value, 
                }
            }
        }
    }

    private bool wheelsAreSet{get{if(Wheels.Contains(null)){return false;}else{return true;}}}
    [TitleGroup(R)]
    [HideIf("wheelsAreSet")]
    [Button("1.Find Wheels"), GUIColor(0f, 1f, 0f)] 
    public void FindWheels()
    {
        GameObject wheelFR = GameObject.FindGameObjectWithTag("WheelFR");
        GameObject wheelFL = GameObject.FindGameObjectWithTag("WheelFL");
        GameObject wheelBR = GameObject.FindGameObjectWithTag("WheelBR");
        GameObject wheelBL = GameObject.FindGameObjectWithTag("WheelBL");


        if(wheelFR != null){Wheel w = wheelFR.GetComponent<Wheel>(); if(w != null){frontWheelR = w;}}
        if(wheelFL != null){Wheel w = wheelFL.GetComponent<Wheel>(); if(w != null){frontWheelL = w;}}
        if(wheelBR != null){Wheel w = wheelBR.GetComponent<Wheel>(); if(w != null){backWheelR = w;}}
        if(wheelBL != null){Wheel w = wheelBL.GetComponent<Wheel>(); if(w != null){backWheelL = w;}}

        if (Wheels.Contains(null))
        {
            Debug.Log("couldnt find all wheels, check if the wheels are tagged correctly ('WheelFR') etc.");
        }
    }
    private bool wheelMeshesAreSet { get { if (WheelMeshes.Contains(null)) { return false; } else { return true; } } }
    [TitleGroup(R)]
    [HideIf("wheelMeshesAreSet")]
    [Button("2.Find WheelMeshes"), GUIColor(0f, 1f, 0f)]
    public void FindWheelMeshes()
    {
        Wheel[] w = Wheels;

        if (w[0] != null) { MeshRenderer mR = w[0].wheelModelTransform.gameObject.GetComponent<MeshRenderer>(); if (mR != null) { frontWheelRMesh = mR; } }
        if (w[1] != null) { MeshRenderer mR = w[1].wheelModelTransform.gameObject.GetComponent<MeshRenderer>(); if (mR != null) { frontWheelLMesh = mR; } }
        if (w[2] != null) { MeshRenderer mR = w[2].wheelModelTransform.gameObject.GetComponent<MeshRenderer>(); if (mR != null) { backWheelRMesh = mR; } }
        if (w[0] != null) { MeshRenderer mR = w[3].wheelModelTransform.gameObject.GetComponent<MeshRenderer>(); if (mR != null) { backWheelLMesh = mR; } }

        if (WheelMeshes.Contains(null))
        {
            Debug.Log("couldnt find all wheelmeshes, check if the wheels have the corret 'wheelModelTransform' assigned, and arent null ");
        }
    }
    // ----------------------------------------- Methods -----------------------------------------


    public bool HasBehavior<T>() where T : CarBehavior// get behavior of the CarBehavior Type from carcontroller - bsp: "cC.HasBehavior<LowRideBehavior>()"
    {
        if (this.gameObject.GetComponent<T>() != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public T GetBehavior<T>() where T : CarBehavior// get behavior of the CarBehavior Type from carcontroller - bsp: "cC.GetBehavior<LowRideBehavior>()"
    {

        T anyT = this.gameObject.GetComponent<T>();
        if (anyT != null)
        {
            return anyT;
        }
        else
        {
            return default(T); // gibt ein objekt zurueck. - vermutlich vom typ Object.
        }
    }

    private void SetAirTime(ref float _inAirTime) 
    {
        //check if is on ground for autoalignCarInAir and controllinputinterference
        if(drivingStateInfo == DrivingState.InAir)
        {
            _inAirTime += Time.deltaTime;
        }
        else
        {
            _inAirTime = 0f;
        }
    }
}



// -------------------------------------------- HELPER STUFF ------------------------------------------------

public enum DrivingState{
    Grounded,
    InAir,
    TwoWheelsGrounded
}