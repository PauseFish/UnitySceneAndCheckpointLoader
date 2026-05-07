using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Claude.ai changed - Manages additive scene loading per checkpoint level.
/// When a level fires it loads its scenes and unloads every other currently
/// loaded scene, except scenes listed in persistentScenes and the scene that
/// contains this SceneLoader GameObject.
///
/// Basic usage:
///   SceneLoader.SetCheckpoint(2);              // load level, don't move player
///   SceneLoader.SetCheckpoint(2, true);        // load level, move player to spawnpoint
///
/// Checkpoint 0 is the default state (game not yet started).
/// </summary>
public class SceneLoader : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Nested Types
    // ─────────────────────────────────────────────────────────────────────────

    [System.Serializable]
    public class SceneEntry
    {
        /// <summary>Claude.ai changed - Runtime scene name, synced from sceneAsset in the editor.</summary>
        public string sceneName = "";

#if UNITY_EDITOR
        /// <summary>Claude.ai changed - Editor-only drag target; sceneName is synced from this.</summary>
        public SceneAsset sceneAsset;
#endif
    }

    [System.Serializable]
    public class LevelEntry
    {
        /// <summary>Claude.ai changed - Display name shown in the inspector, no runtime effect.</summary>
        public string title = "New Level";

        /// <summary>Claude.ai changed - Checkpoint value this level reacts to.</summary>
        public int checkpointValue = 1;

        /// <summary>Claude.ai changed - World position the player is moved to when SetCheckpoint is called with moveToSpawnpoint = true.</summary>
        public Vector3 spawnPoint = Vector3.zero;

        /// <summary>Claude.ai changed - World rotation the player is set to when SetCheckpoint is called with moveToSpawnpoint = true.</summary>
        public Vector3 spawnRotation = Vector3.zero;

        /// <summary>Claude.ai changed - Inspector foldout state.</summary>
        public bool isExpanded = true;

        /// <summary>Claude.ai changed - Scenes to load additively when this level fires.</summary>
        public List<SceneEntry> scenesToLoad = new List<SceneEntry>();

        /// <summary>Claude.ai changed - Scenes kept loaded for this level, in addition to the global persistent list. Loaded if missing.</summary>
        public List<SceneEntry> persistentScenes = new List<SceneEntry>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Static Checkpoint System
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Claude.ai changed - Global checkpoint value. 0 = game not yet started.
    /// </summary>
    public static int CurrentCheckpoint { get; private set; } = 0;

    /// <summary>Claude.ai changed - Fired on every SetCheckpoint() call. Passes the new checkpoint value and whether to move the player.</summary>
    public static event System.Action<int, bool> OnCheckpointChanged;

    /// <summary>
    /// Claude.ai changed - Loads the level matching the given checkpoint value.
    /// </summary>
    /// <param name="value">The checkpoint value to activate.</param>
    public static void SetCheckpoint(int value)
    {
        SetCheckpoint(value, false);
    }

    /// <summary>
    /// Claude.ai changed - Loads the level matching the given checkpoint value.
    /// </summary>
    /// <param name="value">The checkpoint value to activate.</param>
    /// <param name="moveToSpawnpoint">When true the player is teleported to the level's spawnPoint. Use this when loading from a menu or after death.</param>
    public static void SetCheckpoint(int value, bool moveToSpawnpoint)
    {
        if (value == CurrentCheckpoint && !moveToSpawnpoint) return;
        CurrentCheckpoint = value;
        OnCheckpointChanged?.Invoke(value, moveToSpawnpoint);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Serialized Fields
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Claude.ai changed - Tag used to find the player GameObject when moveToSpawnpoint is true.</summary>
    public string playerTag = "Player";

    /// <summary>
    /// Claude.ai changed - Scenes that are never unloaded regardless of which level fires.
    /// The scene containing this SceneLoader is always protected as well.
    /// </summary>
    public List<SceneEntry> persistentScenes = new List<SceneEntry>();

    /// <summary>Claude.ai changed - All level entries. Each entry represents one checkpoint level.</summary>
    public List<LevelEntry> levels = new List<LevelEntry>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        OnCheckpointChanged += HandleCheckpointChanged;
    }

    private void OnDisable()
    {
        OnCheckpointChanged -= HandleCheckpointChanged;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Methods
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleCheckpointChanged(int checkpoint, bool moveToSpawnpoint)
    {
        foreach (var level in levels)
        {
            if (level.checkpointValue != checkpoint) continue;
            ProcessLevel(level, moveToSpawnpoint);
        }
    }

    private void ProcessLevel(LevelEntry level, bool moveToSpawnpoint)
    {
        // Build the set of scene names that must not be unloaded.
        var keepNames = new HashSet<string>();

        // Always protect the scene this SceneLoader lives in.
        keepNames.Add(gameObject.scene.name);

        // Global persistent scenes.
        foreach (var entry in persistentScenes)
            if (!string.IsNullOrEmpty(entry.sceneName))
                keepNames.Add(entry.sceneName);

        // Level-specific persistent scenes.
        foreach (var entry in level.persistentScenes)
            if (!string.IsNullOrEmpty(entry.sceneName))
                keepNames.Add(entry.sceneName);

        // Protect scenes about to be loaded so they aren't immediately unloaded.
        foreach (var entry in level.scenesToLoad)
            if (!string.IsNullOrEmpty(entry.sceneName))
                keepNames.Add(entry.sceneName);

        // Unload every currently loaded scene that is not protected.
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!keepNames.Contains(scene.name))
                SceneManager.UnloadSceneAsync(scene);
        }

        // Load level-specific persistent scenes if not already present.
        foreach (var entry in level.persistentScenes)
            LoadSceneAdditive(entry.sceneName);

        // Load the level's scenes additively.
        foreach (var entry in level.scenesToLoad)
            LoadSceneAdditive(entry.sceneName);

        // Move the player to the spawnpoint if requested.
        if (moveToSpawnpoint)
            MovePlayerToSpawnpoint(level.spawnPoint, level.spawnRotation);
    }

    private void LoadSceneAdditive(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        // Claude.ai changed - Skip scenes that are already loaded to prevent duplicate instances.
        Scene existing = SceneManager.GetSceneByName(sceneName);
        if (existing.IsValid() && existing.isLoaded) return;

        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    /// <summary>Claude.ai changed - Finds the player by tag and teleports it to the given world position and rotation.</summary>
    private void MovePlayerToSpawnpoint(Vector3 position, Vector3 rotation)
    {
        GameObject player = GameObject.FindWithTag(playerTag);

        if (player == null)
        {
            Debug.LogWarning($"[SceneLoader] MoveToSpawnpoint: no GameObject found with tag '{playerTag}'.", this);
            return;
        }

        player.transform.position = position;
        player.transform.eulerAngles = rotation;
    }
}