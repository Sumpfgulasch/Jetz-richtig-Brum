using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.VFX;

public class SceneObjectManager : SerializedMonoBehaviour
{
    // ----------------------------------------- Singleton Implementation -----------------------------------------


    private static SceneObjectManager _instance;

    public static SceneObjectManager Instance { 
        get { // Wenn man auf die Instanz zugreifen moechte
            if (_instance == null) // wenn die Instanz nicht vorhanden ist
            {
                _instance = GameObject.FindObjectOfType<SceneObjectManager>(); // versuche eine Instanz in der Szene zu finden

                if (_instance == null) // wenn sie immernoch null ist
                {
                    GameObject inst = new GameObject("SceneObjectManager"); //erstelle ein Gameobject 
                    _instance = inst.AddComponent<SceneObjectManager>(); // und gebe ihm dieses Script und setze sie als Instanz
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



    // ----------------------------------------- Other Scenerelevant Scene Variables -----------------------------------------



    //the tag "required" shows a warning if something is missing in the inspector.
    [Required] public CarController carController = null; 
    [Required] public VisualEffectAsset wheelSmokeVisualEffectAsset; 


    private void Start() // Add all variables here, to log quickly if something is missing
    {
        if (carController == null) { Debug.LogWarning("The SceneObjectManagers " + "carController" + " is null"); }

        //if (x == null) { Debug.LogWarning("The SceneObjectManagers " + "x" + " is null"); } 
    }
}
