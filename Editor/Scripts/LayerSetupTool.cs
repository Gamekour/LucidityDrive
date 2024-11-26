using UnityEngine;
using UnityEditor;

public class LucidSetupTool : EditorWindow
{
    [MenuItem("LucidityDrive/Automatic Setup")]
    public static void ShowWindow()
    {
        GetWindow<LucidSetupTool>("Automatic setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Automatic Setup", EditorStyles.boldLabel);

        if (GUILayout.Button("Automatic Setup"))
        {
            Setup();
        }
    }

    void Setup()
    {
        SetLayerName(3, "Player");

        SetLayerName(6, "PostProcessing");

        SetLayerName(7, "FlightZone");

        SetLayerName(8, "HideInFirstPerson");

        SetLayerName(9, "GrabbableNoPlayer");

        AddTag("Grabbable");

        Physics.defaultMaxAngularSpeed = 100f;

        Debug.Log("Project set up successfully");
    }

    void SetLayerName(int layerNumber, string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProperty = tagManager.FindProperty("layers");

        if (layersProperty.arraySize > layerNumber)
        {
            SerializedProperty layerSP = layersProperty.GetArrayElementAtIndex(layerNumber);
            layerSP.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
        }
    }

    void AddTag(string newTag)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        int index = tagsProp.arraySize;
        tagsProp.InsertArrayElementAtIndex(index);
        SerializedProperty sp = tagsProp.GetArrayElementAtIndex(index);
        sp.stringValue = newTag;
        tagManager.ApplyModifiedProperties();
    }
}