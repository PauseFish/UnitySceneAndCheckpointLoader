// Claude.ai changed - Place this file inside Assets/Editor/
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Claude.ai changed - Custom inspector for SceneLoaderDebug.
/// Shows the checkpoint int, the moveToSpawnpoint bool, and a button
/// that calls SceneLoader.SetCheckpoint() at runtime.
/// </summary>
[CustomEditor(typeof(LevelController))]
public class SceneLoaderDebugEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("checkpointValue"), new GUIContent("Checkpoint Value"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("moveToSpawnpoint"), new GUIContent("Move To Spawnpoint"));

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(6);

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);

        if (GUILayout.Button("Set Checkpoint", GUILayout.Height(30)))
        {
            LevelController t = (LevelController)target;
            SceneLoader.SetCheckpoint(t.checkpointValue, t.moveToSpawnpoint);
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Enter Play Mode to use this button.", MessageType.Info);

        EditorGUI.EndDisabledGroup();
    }
}
#endif