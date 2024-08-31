using UnityEngine;
using UnityEditor;

public class LayerSetupTool : EditorWindow
{
    [MenuItem("Tools/Layer Setup")]
    public static void ShowWindow()
    {
        GetWindow<LayerSetupTool>("Layer Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Layer Setup Tool", EditorStyles.boldLabel);

        if (GUILayout.Button("Setup Layers"))
        {
            SetupLayers();
        }
    }

    void SetupLayers()
    {
        // Set the name of user layer 3 to "Player"
        SetLayerName(3, "Player");

        // Set the name of user layer 6 to "PostProcessing"
        SetLayerName(6, "PostProcessing");

        // Set the name of user layer 7 to "FlightZone"
        SetLayerName(7, "FlightZone");

        Debug.Log("Layers have been set up successfully.");
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
}