// Claude.ai changed - Place this file inside Assets/Editor/ (create the folder if needed).
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Claude.ai changed - Custom inspector for SceneLoader.
/// Draws a Persistent Scenes box at the top, followed by per-level checkpoint
/// cards each containing a title, checkpoint value, spawnpoint, Load Scenes
/// and Persistent Scenes sub-lists.
/// </summary>
[CustomEditor(typeof(SceneLoader))]
public class SceneLoaderEditor : Editor
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Colours
    // ─────────────────────────────────────────────────────────────────────────

    private static Color HeaderBg => new Color(0.18f, 0.18f, 0.18f, 1f);
    private static Color EntryHeaderBg => new Color(0.22f, 0.22f, 0.22f, 1f);
    private static Color SubHeaderBg => new Color(0.20f, 0.20f, 0.20f, 1f);
    private static Color FooterBg => new Color(0.22f, 0.22f, 0.22f, 1f);
    private static Color RowEvenBg => new Color(0.24f, 0.24f, 0.24f, 1f);
    private static Color RowOddBg => new Color(0.26f, 0.26f, 0.26f, 1f);
    private static Color DividerColor => new Color(0.14f, 0.14f, 0.14f, 1f);
    private static Color PersistentAccent => new Color(0.22f, 0.36f, 0.22f, 1f);

    // ─────────────────────────────────────────────────────────────────────────
    //  Lazy-built GUIStyles
    // ─────────────────────────────────────────────────────────────────────────

    private GUIStyle _headerLabel;
    private GUIStyle _headerCount;
    private GUIStyle _entryFoldout;
    private GUIStyle _subHeaderLabel;
    private GUIStyle _warningLabel;

    private void EnsureStyles()
    {
        if (_headerLabel != null) return;

        _headerLabel = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f, 1f) }
        };

        _headerCount = new GUIStyle(EditorStyles.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(0.60f, 0.60f, 0.60f, 1f) }
        };

        _entryFoldout = new GUIStyle(EditorStyles.foldout)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f, 1f) },
            onNormal = { textColor = new Color(0.85f, 0.85f, 0.85f, 1f) },
            focused = { textColor = new Color(0.85f, 0.85f, 0.85f, 1f) },
            onFocused = { textColor = new Color(0.85f, 0.85f, 0.85f, 1f) },
            active = { textColor = new Color(0.85f, 0.85f, 0.85f, 1f) },
            onActive = { textColor = new Color(0.85f, 0.85f, 0.85f, 1f) }
        };

        _subHeaderLabel = new GUIStyle(EditorStyles.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Normal,
            normal = { textColor = new Color(0.70f, 0.70f, 0.70f, 1f) }
        };

        _warningLabel = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.95f, 0.80f, 0.30f, 1f) }
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Root
    // ─────────────────────────────────────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        EnsureStyles();
        serializedObject.Update();

        // ── Player Tag ───────────────────────────────────────────────────────
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent("Player Tag"));
        SerializedProperty tagProp = serializedObject.FindProperty("playerTag");
        tagProp.stringValue = EditorGUILayout.TagField(tagProp.stringValue);
        EditorGUILayout.EndHorizontal();

        // ── Runtime checkpoint readout ───────────────────────────────────────
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(2);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField(
                new GUIContent("Current Checkpoint", "Read-only — set via SceneLoader.SetCheckpoint(n)."),
                SceneLoader.CurrentCheckpoint
            );
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.Space(8);

        // ── Global Persistent Scenes ─────────────────────────────────────────
        DrawSceneSubList(
            serializedObject.FindProperty("persistentScenes"),
            "Persistent Scenes ()",
            accentColor: PersistentAccent
        );

        EditorGUILayout.Space(8);

        // ── Levels ───────────────────────────────────────────────────────────
        SerializedProperty levelsProp = serializedObject.FindProperty("levels");
        DrawSectionHeader("Checkpoint Events", levelsProp.arraySize);
        EditorGUILayout.Space(2);

        for (int i = 0; i < levelsProp.arraySize; i++)
        {
            DrawLevelEntry(levelsProp.GetArrayElementAtIndex(i), i, levelsProp);
            EditorGUILayout.Space(2);
        }

        DrawGlobalFooter(levelsProp);

        EditorGUILayout.Space(4);
        serializedObject.ApplyModifiedProperties();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Section Header
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawSectionHeader(string title, int count)
    {
        Rect r = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(r, HeaderBg);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1, r.width, 1), DividerColor);
        }

        GUI.Label(new Rect(r.x + 8, r.y + 4, r.width - 50, 18), title, _headerLabel);
        GUI.Label(new Rect(r.xMax - 40, r.y + 4, 36, 16), count.ToString(), _headerCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Level Entry Card
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawLevelEntry(SerializedProperty levelProp, int index, SerializedProperty parentList)
    {
        SerializedProperty titleProp = levelProp.FindPropertyRelative("title");
        SerializedProperty isExpandedProp = levelProp.FindPropertyRelative("isExpanded");
        SerializedProperty checkpointProp = levelProp.FindPropertyRelative("checkpointValue");
        SerializedProperty spawnPointProp = levelProp.FindPropertyRelative("spawnPoint");
        SerializedProperty spawnRotationProp = levelProp.FindPropertyRelative("spawnRotation");
        SerializedProperty loadListProp = levelProp.FindPropertyRelative("scenesToLoad");
        SerializedProperty levelPersistentProp = levelProp.FindPropertyRelative("persistentScenes");

        // ── Entry header ─────────────────────────────────────────────────────
        Rect hdr = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(hdr, EntryHeaderBg);
            EditorGUI.DrawRect(new Rect(hdr.x, hdr.yMax - 1, hdr.width, 1), DividerColor);
        }

        string foldoutLabel = string.IsNullOrEmpty(titleProp.stringValue) ? "Level" : titleProp.stringValue;

        isExpandedProp.boolValue = EditorGUI.Foldout(
            new Rect(hdr.x + 6, hdr.y + 4, hdr.width - 120, 15),
            isExpandedProp.boolValue,
            foldoutLabel,
            true,
            _entryFoldout
        );

        GUI.Label(
            new Rect(hdr.xMax - 104, hdr.y + 4, 82, 15),
            $"Checkpoint: {checkpointProp.intValue}",
            _headerCount
        );

        if (DrawFlatButton(new Rect(hdr.xMax - 18, hdr.y + 3, 18, 16), "-"))
        {
            parentList.DeleteArrayElementAtIndex(index);
            return;
        }

        if (!isExpandedProp.boolValue) return;

        // ── Field rows ───────────────────────────────────────────────────────

        DrawFieldRow(0, "Title", (valueRect) =>
        {
            titleProp.stringValue = EditorGUI.TextField(valueRect, titleProp.stringValue);
        }, rowHeight: 24);

        DrawFieldRow(1, "Checkpoint", (valueRect) =>
        {
            checkpointProp.intValue = EditorGUI.IntField(valueRect, checkpointProp.intValue);
        }, rowHeight: 24);

        DrawFieldRow(2, "Spawn Point", (valueRect) =>
        {
            spawnPointProp.vector3Value = EditorGUI.Vector3Field(valueRect, GUIContent.none, spawnPointProp.vector3Value);
        }, rowHeight: 24);

        DrawFieldRow(3, "Spawn Rotation", (valueRect) =>
        {
            spawnRotationProp.vector3Value = EditorGUI.Vector3Field(valueRect, GUIContent.none, spawnRotationProp.vector3Value);
        }, rowHeight: 24);

        EditorGUILayout.Space(10);

        // ── Sub-lists ────────────────────────────────────────────────────────
        DrawSceneSubList(loadListProp, "Load Scenes ()");
        EditorGUILayout.Space(5);
        DrawSceneSubList(levelPersistentProp, "Persistent Scenes ()", accentColor: PersistentAccent);
        EditorGUILayout.Space(4);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Generic Field Row
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawFieldRow(int rowIndex, string label, System.Action<Rect> drawValue, float rowHeight = 22)
    {
        Rect row = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(row, rowIndex % 2 == 0 ? RowEvenBg : RowOddBg);

        EditorGUI.LabelField(
            new Rect(row.x + 10, row.y + 3, 90, 16),
            label,
            EditorStyles.miniLabel
        );

        drawValue(new Rect(row.x + 104, row.y + 3, row.width - 114, rowHeight - 6));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Scene Sub-List
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawSceneSubList(SerializedProperty listProp, string title, Color? accentColor = null)
    {
        Color resolvedAccent = accentColor ?? SubHeaderBg;

        // Sub-header
        Rect subHdr = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(subHdr, resolvedAccent);
            EditorGUI.DrawRect(new Rect(subHdr.x, subHdr.y, subHdr.width, 1), DividerColor);
            EditorGUI.DrawRect(new Rect(subHdr.x, subHdr.yMax - 1, subHdr.width, 1), DividerColor);
        }

        GUI.Label(
            new Rect(subHdr.x + 10, subHdr.y + 3, subHdr.width - 50, 14),
            title,
            _subHeaderLabel
        );
        GUI.Label(
            new Rect(subHdr.xMax - 40, subHdr.y + 3, 36, 14),
            listProp.arraySize.ToString(),
            new GUIStyle(_subHeaderLabel) { alignment = TextAnchor.MiddleRight }
        );

        // Scene rows
        for (int i = 0; i < listProp.arraySize; i++)
        {
            SerializedProperty entry = listProp.GetArrayElementAtIndex(i);
            SerializedProperty sceneNameP = entry.FindPropertyRelative("sceneName");
            SerializedProperty sceneAssetP = entry.FindPropertyRelative("sceneAsset");

            Rect row = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(row, i % 2 == 0 ? RowEvenBg : RowOddBg);

            EditorGUI.BeginChangeCheck();
            SceneAsset newAsset = (SceneAsset)EditorGUI.ObjectField(
                new Rect(row.x + 10, row.y + 3, row.width - 38, 18),
                sceneAssetP.objectReferenceValue,
                typeof(SceneAsset),
                false
            );
            if (EditorGUI.EndChangeCheck())
            {
                sceneAssetP.objectReferenceValue = newAsset;
                sceneNameP.stringValue = newAsset != null ? newAsset.name : "";
            }

            if (DrawFlatButton(new Rect(row.xMax - 25, row.y + 3, 20, 18), "-"))
            {
                listProp.DeleteArrayElementAtIndex(i);
                break;
            }

            // Build Settings warning
            SceneAsset current = sceneAssetP.objectReferenceValue as SceneAsset;
            if (current != null && !IsInBuildSettings(current))
            {
                Rect wRow = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(wRow, i % 2 == 0 ? RowOddBg : RowEvenBg);
                GUI.Label(
                    new Rect(wRow.x + 10, wRow.y + 2, wRow.width - 50, 17),
                    "⚠  Not in Build Settings",
                    _warningLabel
                );
                if (GUI.Button(new Rect(wRow.xMax - 40, wRow.y + 2, 34, 14), "Add", EditorStyles.miniButton))
                    AddToBuildSettings(current);
            }
        }

        // Footer with + / - flat buttons
        Rect footer = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(footer, FooterBg);
            EditorGUI.DrawRect(new Rect(footer.x, footer.y, footer.width, 1), DividerColor);
        }

        if (DrawFlatButton(new Rect(footer.xMax - 47, footer.y + 4, 20, 18), "+"))
        {
            listProp.arraySize++;
            var e = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
            e.FindPropertyRelative("sceneName").stringValue = "";
            e.FindPropertyRelative("sceneAsset").objectReferenceValue = null;
        }

        GUI.enabled = listProp.arraySize > 0;
        if (DrawFlatButton(new Rect(footer.xMax - 25, footer.y + 4, 20, 18), "-"))
            listProp.DeleteArrayElementAtIndex(listProp.arraySize - 1);
        GUI.enabled = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Global Footer  —  "Add" / "Remove" flat buttons
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawGlobalFooter(SerializedProperty levelsProp)
    {
        // Claude.ai changed - Switched to DrawFlatButton so height is fully respected.
        Rect footer = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(footer, FooterBg);
            EditorGUI.DrawRect(new Rect(footer.x, footer.y, footer.width, 1), DividerColor);
        }

        if (DrawFlatButton(new Rect(footer.xMax - 164, footer.y + 4, 80, 30), "Add"))
        {
            levelsProp.arraySize++;
            InitLevel(levelsProp.GetArrayElementAtIndex(levelsProp.arraySize - 1), levelsProp.arraySize);
        }

        GUI.enabled = levelsProp.arraySize > 0;
        if (DrawFlatButton(new Rect(footer.xMax - 80, footer.y + 4, 80, 30), "Remove"))
            levelsProp.DeleteArrayElementAtIndex(levelsProp.arraySize - 1);
        GUI.enabled = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Flat Button  —  fully manual draw, no inherited style padding
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Claude.ai changed - Draws a flat button using raw rects so height and
    /// text centering are not affected by EditorStyles.miniButton padding.
    /// </summary>
    private bool DrawFlatButton(Rect rect, string text)
    {
        bool pressed = false;

        if (Event.current.type == EventType.Repaint)
        {
            Color bg = rect.Contains(Event.current.mousePosition)
                ? new Color(0.38f, 0.38f, 0.38f, 1f)
                : new Color(0.30f, 0.30f, 0.30f, 1f);

            EditorGUI.DrawRect(rect, bg);

            // Border
            Color border = new Color(0.12f, 0.12f, 0.12f, 1f);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), border);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), border);

            // Centred label — adjust fontSize to resize glyph, rect.y offset to nudge vertically
            GUIStyle centred = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                normal = { textColor = new Color(0.80f, 0.80f, 0.80f, 1f) }
            };
            GUI.Label(new Rect(rect.x, rect.y - 1.5f, rect.width, rect.height), text, centred);
        }

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            pressed = true;
            Event.current.Use();
        }

        return pressed;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Claude.ai changed - Initialises a freshly added LevelEntry to safe defaults.</summary>
    private void InitLevel(SerializedProperty prop, int index)
    {
        prop.FindPropertyRelative("title").stringValue = "New Level";
        prop.FindPropertyRelative("checkpointValue").intValue = index;
        prop.FindPropertyRelative("spawnPoint").vector3Value = Vector3.zero;
        prop.FindPropertyRelative("spawnRotation").vector3Value = Vector3.zero;
        prop.FindPropertyRelative("isExpanded").boolValue = true;
        prop.FindPropertyRelative("scenesToLoad").arraySize = 0;
        prop.FindPropertyRelative("persistentScenes").arraySize = 0;
    }

    private bool IsInBuildSettings(SceneAsset asset)
    {
        string path = AssetDatabase.GetAssetPath(asset);
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            if (s.path == path) return true;
        return false;
    }

    private void AddToBuildSettings(SceneAsset asset)
    {
        string path = AssetDatabase.GetAssetPath(asset);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif