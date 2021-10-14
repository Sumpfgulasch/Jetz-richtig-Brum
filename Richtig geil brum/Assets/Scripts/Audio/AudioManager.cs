using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AudioHelm;

public class AudioManager : MonoBehaviour
{
    // ----------------------------------------- Singleton Implementation -----------------------------------------


    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        { // Wenn man auf die Instanz zugreifen moechte
            if (_instance == null) // wenn die Instanz nicht vorhanden ist
            {
                _instance = GameObject.FindObjectOfType<AudioManager>(); // versuche eine Instanz in der Szene zu finden

                if (_instance == null) // wenn sie immernoch null ist
                {
                    GameObject inst = new GameObject("AudioManager"); //erstelle ein Gameobject 
                    _instance = inst.AddComponent<AudioManager>(); // und gebe ihm dieses Script und setze sie als Instanz
                }
            }

            return _instance; // gib die Instanz zueurck
        }
    }
    private void Awake()
    {
        if (_instance != null && _instance != this) //wenn die die Instanz nicht null ist und nicht diese Instanz -> also irgendeine andere instanz
        {
            Destroy(this.gameObject); // dann zerstoere dieses Gameobject, denn es gibt schon eins
        }
        else // ansonsten 
        {
            _instance = this; // ist diese instanz die richtige instanz
        }

        //DontDestroyOnLoad(_instance.gameObject); //  game object will not be destroyed on sceneChange //Keine ahnung warum da eine fehlermeldung kommt, luuul
    }



    // ----------------------------------------- Der Code beginnt hier -----------------------------------------

    public HelmController engineController1;
    public int engine1Note = 36;        // Midi-value (36 = C1) [0-127]


    IEnumerator Start()
    {
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
