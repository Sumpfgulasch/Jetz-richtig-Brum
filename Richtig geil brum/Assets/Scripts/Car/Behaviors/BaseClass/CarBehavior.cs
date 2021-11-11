using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;

[Serializable]
[RequireComponent(typeof(CarController))] // every carBehavior needs a CarController on the same Gameobject.
public abstract class CarBehavior : SerializedMonoBehaviour // abstract class, so it only can be inherited from, and not be instantiated by its own., inherits from SerializedMonobehavior, so everything inherits from it.
{
    const string B = "Base";
    /// <summary>
    /// Enables/Disables The Behavior entirely. has to be implemented into the inherited Script.
    /// </summary>
    [TitleGroup(B)] public bool EnabledBehavior = true;
    /// <summary>
    /// ExecutionPriority provides a way to Order the BehaviorExecution. Scripts with the Same Value will be executed randomly after another.
    /// </summary>
    [TitleGroup(B)] public int ExecutionPriority = 0;
    /// <summary>
    /// A Reference to the CarController this Behavior Belongs to, for easy and guaranteed access
    /// </summary>
    private CarController carController = null;
    public CarController cC { get => carController; private set => carController = value; }
    /// <summary>
    /// A bool to check if a Script was initialized Successfully, it is the value that is returned by the SetRequirements value. It can be used to prevent the game from crashing 
    /// if behaviors arent initialized completely.
    /// </summary>
    [TitleGroup(B)] public bool initializedSuccessfully = false;

    /// <summary>
    /// All Classes that inherit from CarBehavior should not override the Start Method. It calles the "SetRequirements()" method and tries to find the CarController.
    /// </summary>
    public void Start()
    {
        cC = this.gameObject.GetComponent<CarController>(); // Try to Find CarController on this GameObject

        if (cC != null)
        {
            initializedSuccessfully = SetRequirements();
        }
        else 
        {
            Debug.LogWarning(this.name + ": this behavior has no assigned CarController, Das ist garnicht so gut, hier ist was schief gelaufen.");
        }
    }
    /// <summary>
    /// Inititalizes all Requirements to work Properly, Requirements are - References, initial Values ,etc. It should return true if it ran succsessfully, and false if it didnt.
    /// </summary>
    public abstract bool SetRequirements();

    /// <summary>
    /// is Used to Execute the behavior. The Car Controller Executes the Behaviors in order, by their ExecutionPriority. Executions shouldnt be made in the OnSchlagMichTod() InputManager Methods - Only
    /// Execute relevant bools/Values should be set there.
    /// ExecuteBehavior takes a Function which returns a Bool, so that complex Rules can be setup in the CarController.
    /// </summary>
    /// <param name="_shouldExecute"></param>
    public abstract void ExecuteBehavior(Func<bool> _shouldExecute);
}

//BEHAVIOR SCRIPT AUFBAU: - copy bei erstellung neues scriptes das dieses hier erbt.

//[RequireComponent(typeof(TypenDenDasScriptBraucht))]

//VARIABLES

//------------------------ SETUP

//SetRequirements()

//------------------------ INPUT HANDLING

//OnSchlagMichTod() ->Inputmanager befehle

//------------------------ BEHAVIOR

//ExecuteBehavior()

//WeitereExecuteRelevante functionen