using UnityEngine;
using UnityEditor;

public class LayerSetupTool : EditorWindow
{
    [MenuItem("LucidityDrive/Setup Tags and Layers")]
    public static void ShowWindow()
    {
        GetWindow<LayerSetupTool>("Setup Tags and Layers");
    }

    void OnGUI()
    {
        GUILayout.Label("Tags and Layers Setup Tool", EditorStyles.boldLabel);

        if (GUILayout.Button("Setup Tags and Layers"))
        {
            Setup();
        }
    }

    void Setup()
    {
        // Set the name of user layer 3 to "Player"
        SetLayerName(3, "Player");

        // Set the name of user layer 6 to "PostProcessing"
        SetLayerName(6, "PostProcessing");

        // Set the name of user layer 7 to "FlightZone"
        SetLayerName(7, "FlightZone");

        AddTag("Grabbable");

        Debug.Log("Tags and Layers have been set up successfully.");
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