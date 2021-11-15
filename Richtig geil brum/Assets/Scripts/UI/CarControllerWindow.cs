using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;
using System;
using System.Reflection;

public class CarControllerWindow : OdinEditorWindow
{
    private CarControllerWindow cCW;
    private VisualElement _rootElement;
    private VisualTreeAsset _visualTree = null;


    private SerializedObject carControllerSerialized;
    private CarController selectedCarController;
    private CarController presetCarController;
    private ObjectField carControllerObjectField;

    [MenuItem("Car/CarBehaviorEditor")]
    public static void ShowWindow()
    {
        CarControllerWindow cCW = (CarControllerWindow)EditorWindow.GetWindow(typeof(CarControllerWindow));
        cCW.Show();
    }
    protected override void OnEnable()
    {
        Debug.Log("OnEnable");

        _visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/UI/CarControllerEditorWindow.uxml");
        if (_visualTree == null)
        {
            Debug.Log("couldnt find a visual tree");
        }
        else
        {
            TemplateContainer treeAsset = _visualTree.CloneTree();
            rootVisualElement.Add(treeAsset);
        }


        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/UI/CarControllerEditorWindowStyle.uss");
        if (styleSheet == null)
        {
            Debug.Log("couldnt find a StyleSheet");
        }
        else
        {
            rootVisualElement.styleSheets.Add(styleSheet);
        }


        CreateCarList();
        CreateBehaviorList();

        ////Assign a Field to a script value 
        //CarController cC = FindObjectsOfType<CarController>().FirstOrDefault();
        //// Get a reference to the field from UXML and assign it its value.
        //Toggle toggle = new Toggle();
        //toggle.value = true;
        //cC.TestBool = toggle.value;
        //// Mirror value of uxml field into the C# field.
        //toggle.RegisterCallback<ChangeEvent<bool>>((evt) =>
        //{
        //    cC.TestBool = evt.newValue;
        //});

        ////create a new Button
        //Button button = new Button();
        //button.name = "newButtonElementABC";

        ////create a ObjectField of type CarController.
        //ObjectField carControllerObjectField = new ObjectField();
        //carControllerObjectField.name = "CarController";
        //carControllerObjectField.objectType = typeof(CarController);

        ////create a ObjectField of type CarController.
        //FloatField floatField = new FloatField();
        //floatField.name = "FloatField";

        ////assign it to any Level of hierachy on the Editor
        //VisualElement carButtonList = rootVisualElement.Q<VisualElement>("CarList"); // damit lassen sich Dinge suchen. (auch nach attributen suchbar)
        //carButtonList.Add(button);
        //carButtonList.Add(carControllerObjectField);
        //carButtonList.Add(floatField);
        //carButtonList.Add(toggle);


        //// Get the Type and MemberInfo.
        //Type MyType = Type.GetType("CarController");
        //MemberInfo[] Mymemberinfoarray = MyType.GetMembers();

        //// Get the MemberType method and display the elements.
        //Debug.Log("\nThere are {0} members in "+ Mymemberinfoarray.GetLength(0));
        //Debug.Log("{0}."+ MyType.FullName);

        //for (int counter = 0; counter < Mymemberinfoarray.Length; counter++)
        //{
        //    Debug.Log("\n" + counter + ". "
        //        + Mymemberinfoarray[counter].Name
        //        + " Member type - " +
        //        Mymemberinfoarray[counter].MemberType.ToString());
        //}


        //BUTTONS
        //// 1
        //Button newButton = rootVisualElement.Q<Button>("NewButton");
        //Button clearButton = rootVisualElement.Q<Button>("ClearButton");
        //Button deleteButton = rootVisualElement.Q<Button>("DeleteButton");

        //// 2
        //newButton.clickable.clicked += () =>
        //{
        //    if (presetManager != null)
        //    {
        //        Preset newPreset = new Preset();
        //        presetManager.presets.Add(newPreset);

        //        EditorUtility.SetDirty(presetManager);

        //        PopulatePresetList();
        //        BindControls();
        //    }
        //};

        //// 3
        //clearButton.clickable.clicked += () =>
        //{
        //    if (presetManager != null && selectedPreset != null)
        //    {
        //        selectedPreset.color = Color.black;
        //        selectedPreset.animationSpeed = 1;
        //        selectedPreset.objectName = "Unnamed Preset";
        //        selectedPreset.isAnimating = true;
        //        selectedPreset.rotation = Vector3.zero;
        //        selectedPreset.size = Vector3.one;
        //    }
        //};

        //// 4
        //deleteButton.clickable.clicked += () =>
        //{
        //    if (presetManager != null && selectedPreset != null)
        //    {
        //        presetManager.presets.Remove(selectedPreset);
        //        PopulatePresetList();
        //        BindControls();
        //    }
        //};

        //PopulatePresetList();
        //SetupControls();

        //CreateCarControllerList();

        //base.OnEnable(); // to odinstuff.

    }

    private void CreateCarList()
    {
        CarController[] cCS = FindObjectsOfType<CarController>(); // get all carControllers in Scene.

        VisualElement carButtonList = rootVisualElement.Q<VisualElement>("CarList"); //Find(Query) the Visual Element which holds the CarList. and save it in a variable.

        foreach (CarController cC in cCS) //gehe durch alle CarController.
        {
            
            Button cCButton = new Button(); //create a new Button
            //TODO - add a style.
            cCButton.text = cC.transform.name; // assign the Text to the button.
            cCButton.name = cC.transform.name; // name the button (for query)
            carButtonList.Add(cCButton); // add it to the car list.
        }


    }

    private void CreateBehaviorList()
    {
        //assign it to any Level of hierachy on the Editor
        VisualElement behaviorList = rootVisualElement.Q<VisualElement>("BehaviorList"); // damit lassen sich Dinge suchen. (auch nach attributen suchbar)

        //Get the car Controller
        CarController cC = FindObjectsOfType<CarController>().FirstOrDefault();
        Debug.Log("ERSETZEN DURCH CURRENT SELECTED CAR CONTROLLER");// ersetzen durch currentCarController.

        // Get the Type and MemberInfo.
        Type cCType = cC.GetType();//Variant 1 :Type.GetType("CarController"); // Variant 2 :cC.GetType();
        MemberInfo[] cCMemberInfo = cCType.GetMembers();

        // Get the MemberType method and display the elements.
        FieldInfo carBehaviorField = cCType.GetField("carBehaviors"); // get the field carBehaviors from the CarController.
        if (carBehaviorField != null) // wenn es ein carBehavior Attribut gibt
        {
            Debug.Log("Found carBehaviorField"); 
            Debug.Log(carBehaviorField);
            Debug.Log("ERSETZEN DURCH: Soll sich aktualisieren wenn sich die Liste veraendert.");
            if (carBehaviorField.FieldType == typeof(List<CarBehavior>)) // wenn es eine Liste vom Typen CarBehavior Ist. 
            {
                List<CarBehavior> carBehaviorList = carBehaviorField.GetValue(cC) as List<CarBehavior>; // caste das carBehaviorField in die dementsprechende Liste.
                foreach (var cB in carBehaviorList) // Fuer jedes Element in der Liste
                {
                    Label l = new Label(); // erstelle ein Label
                    Toggle t = new Toggle(); // erstellt ein Toggle
                    l.text = "das Behavior " + cB.ToString();// bennene das Label entsprechend des Behaviors in der Liste
                    t.text = "ist initialisiert";
                    t.value = cB.initializedSuccessfully; // initialize it to the right value.
                    t.SetEnabled(false); // graut den toggle aus, sodass er keinen Input entgegen nimmt.

                    SerializedObject serializedObject = new UnityEditor.SerializedObject(cB);
                    SerializedProperty serializedPropertyInit = serializedObject.FindProperty("initializedSuccessfully");
                    t.BindProperty(serializedPropertyInit); // bind the Property to the toggle

                    if (behaviorList != null) // wenn das VisualElement in der uxml Datei gefunden wurde.
                    {
                        behaviorList.Add(l); // fuege das Label hinzu.
                        behaviorList.Add(t); // fuege das Label hinzu.
                    }

                }
            }
        }



        //foreach (var item in ))
        //{
        //    carButtonList.Add(button);
        //}

        ////////////////////////////////////////
        //// When you want the Type of a class
        //Type myClassType = typeof(MyClass);

        //// When you want the Type of a variable
        //Type myClassType = myClassInstance.GetType();

        //// Get the field named "MyField"
        //FieldInfo myFieldInfo = myClassType.GetField("MyField");

        //// Get the field named "MyProperty"
        //PropertyInfo myPropertyInfo = myClassType.GetProperty("MyProperty");

        //// Get the method/function named "MyMethod"
        //MethodInfo myMethodInfo = myClassType.GetMethod("MyMethod");

        //// Read or write a field from an instance of MyClass
        //myFieldInfo.GetValue(myClassInstance);
        //myFieldInfo.SetValue(myClassInstance, 123);

        //// Read or write a property from an instance of MyClass
        //// The last parameter is null for non-indexed properties
        //myPropertyInfo.GetValue(myClassInstance, null);
        //myPropertyInfo.SetValue(myClassInstance, 123, null);

        //// Call a method on an instance of MyClass
        //object[] parameters = new object[] { 1, 2, 3 };
        //myMethodInfo.Invoke(myClassInstance, parameters);
        ////////////////////////////////////////

    }


    private void SetupControls() //Adds actions to the buttons so something happens when you click them.
    {

    }

    private void PopulatePresetList() //Populates the List view with VisualElements that represent the presets.
    {

    }

    private void LoadPreset(int elementID) // Given an ID, the script will load a preset into the editor window.
    {

    }

    private void BindControls() // Binds the selected preset’s values to the editor window controls.
    {

    }


    //public void CreateCarControllerList()
    //{
    //    CarController[] carControllers = FindObjectsOfType<CarController>();

    //    ListView carControllerList = rootVisualElement.Query<ListView>("carControllerList").First();
    //    carControllerList.makeItem = () => new Label();
    //    carControllerList.bindItem = (element, i) => (element as Label).text = carControllers[i].name;

    //    carControllerList.itemsSource = carControllers;
    //    carControllerList.itemHeight = 16;
    //    carControllerList.selectionType = SelectionType.Single;

    //    carControllerList.onSelectionChange += (enumerable) =>
    //    {
    //        foreach (Object item in enumerable)
    //        {
    //            Box carControllerBoxInfo = rootVisualElement.Query<Box>("carControllerInfo").First();
    //            carControllerBoxInfo.Clear();

    //            CarController carController = item as CarController;
    //            SerializedObject serializedCarController = new SerializedObject(carController);
    //            SerializedProperty carControllerProperty = serializedCarController.GetIterator();
    //            carControllerProperty.Next(true);

    //            while (carControllerProperty.NextVisible(false))
    //            {
    //                PropertyField prop = new PropertyField(carControllerProperty);

    //                prop.SetEnabled(carControllerProperty.name != "m_Script");
    //                prop.Bind(serializedCarController);
    //                carControllerBoxInfo.Add(prop);

    //                if (carControllerProperty.name == "Something")
    //                {
    //                    //prop.RegisterCallback<ChangeEvent<UnityEngine.Object>>((changeEvt) => LoadCardImage(card.cardImage.texture));

    //                }
    //            }

    //            //LoadCardImage(card.cardimage.texture);
    //        }
    //    };
    //    carControllerList.Refresh();
    //}
    public void CreateGUI()
    {
        Debug.Log("CreatedGUI");
    }
    private void OnGUI()
    {
       
    }

}
