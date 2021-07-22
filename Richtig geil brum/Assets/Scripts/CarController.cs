using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnThrust(InputValue value)
    {

        print("onThrust: " + value.Get<float>());
    }

    public void OnSteer(InputValue value)
    {
        print("onSteer: " + value.Get<float>());
    }

    public void OnLowRide(InputValue value)
    {
        print("onLowRide: " + value.Get<Vector2>());
    }
}
