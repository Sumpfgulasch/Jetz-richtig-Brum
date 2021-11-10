using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine.UI;
using TMPro;
using UnityEngine.VFX;

public class CollectableMechanic : Collectable
{
    const string C = "Type of the Collectable";

    CarController cC;

    [TitleGroup(C)] public MechanicTypes mechanicType = MechanicTypes.DriveForward;

    private bool enable = true;
    [TitleGroup(C), OdinSerialize] public override bool Enable { get => enable; set => enable = value; } // enable or disable the mechanic.


    public new void Start() //new keyword, to NOT override the baseclasses Start Method.
    {
        base.Start(); // call the base classes start Method. (which currently finds the collider and sets it to be a trigger - 11-07-2021)
                      
        // --------------INITITALIZE CAR CONTROLLER
        if (cC == null)// get CarController from Scene (only works if there is only one)
        {
            CarController[] allCarControllers = (CarController[])GameObject.FindObjectsOfType(typeof(CarController)); // find all CarControllers
            if (allCarControllers.Length > 0) // there is a carController in the scene
            { 
                if(allCarControllers.Length > 1) // if there are more than one car Controllers
                {
                    Debug.LogWarning("There are multiple CarControllers in the scene, this Script wont work properly");
                }
                cC = allCarControllers[0]; // assign the first CarController
            }
            else // if there is no CarController found
            {
                Debug.LogWarning("there is no CarController reference");
            }
        }

        // --------------SPAWN A TEXT WHICH DISPLAYS THE CURRENT MECHANIC
        GameObject textGo = Instantiate(SceneObjectManager.Instance.BasicTextObject);
        textGo.transform.SetParent(this.transform);
        textGo.transform.position = this.transform.position;
        textGo.transform.position += Vector3.up * 0.75f;
        TextMeshProUGUI tmpGO = textGo.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        tmpGO.text = mechanicType.ToString();

    }
    public override void OnCollect(bool _enable)
    {
        switch (mechanicType)
        {
            //case MechanicTypes.DriveForward:
            //    cC.UseDriveForward = _enable;
            //    break;
            //case MechanicTypes.DriveBackwards:
            //    cC.UseDriveBackward = _enable;
            //    break;
            //case MechanicTypes.MagnetShort:
            //    cC.UseMagnet = _enable;
            //    cC.magnetMaxTime = 3f;
            //    break;
            //case MechanicTypes.MagnetMedium:
            //    cC.UseMagnet = _enable;
            //    cC.magnetMaxTime = 8f;
            //    break;
            //case MechanicTypes.MagnetLong:
            //    cC.UseMagnet = _enable;
            //    cC.magnetMaxTime = 15f;
            //    break;
            //case MechanicTypes.InAirControl:
            //    cC.UseInAirControl = _enable;
            //    break;
            //case MechanicTypes.SteerLeft:
            //    cC.UseSteerLeft = _enable;
            //    break;
            //case MechanicTypes.SteerRight:
            //    cC.UseSteerRight = _enable;
            //    break;
            //case MechanicTypes.LowRideFrontal:
            //    cC.UseLowRideFrontal = _enable;
            //    break;
            //case MechanicTypes.LowRideBack:
            //    cC.UseLowRideBack = _enable;
            //    break;
            //default:
            //    Debug.Log("Collectable is of no type");
            //    break;
        }

        //SPAWN AND KILL PARTICLE.
        SpawnAndKillParticle(this.transform.position, SceneObjectManager.Instance.collectableMechanicVisualEffectAsset, 5f);
    }


    public enum MechanicTypes
    {
        DriveForward,
        DriveBackwards,
        MagnetShort,
        MagnetMedium,
        MagnetLong,
        InAirControl,
        SteerRight,
        SteerLeft,
        LowRideFrontal,
        LowRideBack
    }
}