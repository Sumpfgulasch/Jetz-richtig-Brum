using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//Require Component creates a ColliderComponent when this script is attached to any Object.
[RequireComponent(typeof(Collider))]
// collectable is an abstract class, that means that it cant be instantiated(not put on any GameObject, and cant be created with "new" keyword), it only can be Inherited from.
public abstract class Collectable : SerializedMonoBehaviour
{
    public abstract bool Enable { get; set; }
    public void Start()
    {
        //set the collider as a trigger.
        Collider col = this.GetComponent<Collider>();
        if (col == null)
        {
            Debug.Log(this.gameObject.name + ": hier sollte eigentlich ein collider drauf sein. RIPP");
        }
        else
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        OnCollect(Enable); // when the trigger is entered activate the abstract method onCollect.
        Debug.Log("Collected Me");
        Destroy(this.gameObject);
    }

    public abstract void OnCollect(bool _enable); // OnCollect is abstract -> every class that inherits from collectable has to implement the OnCollect function.
}

