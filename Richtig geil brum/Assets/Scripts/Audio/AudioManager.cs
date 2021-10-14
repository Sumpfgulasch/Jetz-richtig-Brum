using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AudioHelm;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public HelmController engineController1;
    public int engine1Note = 36;        // Midi-value (36 = C1) [0-127]


    IEnumerator Start()
    {
        instance = this;

        yield return new WaitForSeconds(1f);
        StartEngineSound(engineController1, engine1Note, true);
        yield return null;
    }


    private void FixedUpdate()
    {
        float velocity = SceneObjectManager.Instance.carController.RB.velocity.magnitude;
        float targetValue = Mathf.Clamp01(velocity.Remap(0, SceneObjectManager.Instance.carController.maxSpeed, 0, 1f));
        //float targetValue = Mathf.Abs(CarController.instance.thrustValue);

        ControlPitchByParameter(engineController1, targetValue);
    }


    // --------------------------------------------- PRIVATE METHODS ---------------------------------------------


    private void StartEngineSound(HelmController controller, int note, bool addOctave)
    {
        controller.NoteOn(note);

        if (addOctave)
        {
            controller.NoteOn(note + 12);
        }
    }

    private void ControlPitchByParameter(HelmController controller, float value)
    {
        controller.SetPitchWheel(value);
    }
}
