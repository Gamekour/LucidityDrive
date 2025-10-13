using UnityEngine;
using UnityEditor;

namespace LucidityDrive
{
    public class LucidSetupTool : EditorWindow
    {
        private const int LAYER_PLAYER = 3;
        private const int LAYER_FLIGHTZONE = 6;
        private const int LAYER_GRABBABLENOPLAYER = 7;

        [MenuItem("LucidityDrive/Automatic Setup")]
        public static void Setup()
        {
            SetLayerName(LAYER_PLAYER, "Player");
            SetLayerName(LAYER_FLIGHTZONE, "FlightZone");
            SetLayerName(LAYER_GRABBABLENOPLAYER, "GrabbableNoPlayer");

            AddTag("Grabbable");

            Physics.IgnoreLayerCollision(LAYER_PLAYER, LAYER_PLAYER);
            Physics.IgnoreLayerCollision(LAYER_PLAYER, LAYER_GRABBABLENOPLAYER);
            Physics.IgnoreLayerCollision(LAYER_GRABBABLENOPLAYER, LAYER_GRABBABLENOPLAYER);

            Physics.defaultMaxAngularSpeed = 100f;

            Debug.Log("Project set up successfully");
        }

        public static void SetLayerName(int layerNumber, string layerName)
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

        public static void AddTag(string newTag)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty existingTag = tagsProp.GetArrayElementAtIndex(i);
                if (existingTag.stringValue == newTag)
                    return;
            }

            int index = tagsProp.arraySize;
            tagsProp.InsertArrayElementAtIndex(index);
            SerializedProperty sp = tagsProp.GetArrayElementAtIndex(index);
            sp.stringValue = newTag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
