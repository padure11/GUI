#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelStripper
{
    // Mechanics that exist as MonoBehaviours in the scene.
    // Strings are full class names found on GameObjects.
    private static readonly string[] AllMechanicScripts = new[]
    {
        "Lever",
        "Portal",
        "ObjectiveButton",
        "ElectricZone",
        "PressButton",
        "RoboArm",
    };

    // Mechanics that come from prefab assets (no dedicated script component).
    // Identified by their source prefab name in the project.
    private static readonly string[] AllMechanicPrefabNames = new[]
    {
        "Crate",
        "PressurePlate",
        "SpringPlatform",
        "StandButton",
    };

    [MenuItem("Tools/Levels/Setup Level 1 (movement only)")]
    public static void SetupLevel1()
    {
        StripLevel(
            "Assets/Scenes/Level1.unity",
            stripScripts: AllMechanicScripts,
            stripPrefabs: AllMechanicPrefabNames
        );
    }

    [MenuItem("Tools/Levels/Setup Level 2 (+ push)")]
    public static void SetupLevel2()
    {
        // Keep: Crate, PressurePlate. Strip everything else.
        StripLevel(
            "Assets/Scenes/Level2.unity",
            stripScripts: new[] { "Lever", "Portal", "ObjectiveButton", "ElectricZone", "PressButton", "RoboArm" },
            stripPrefabs: new[] { "SpringPlatform", "StandButton" }
        );
    }

    [MenuItem("Tools/Levels/Setup Level 3 (+ pull/lever)")]
    public static void SetupLevel3()
    {
        // Keep: Crate, PressurePlate, Lever, ElectricZone.
        StripLevel(
            "Assets/Scenes/Level3.unity",
            stripScripts: new[] { "Portal", "ObjectiveButton", "PressButton", "RoboArm" },
            stripPrefabs: new[] { "SpringPlatform", "StandButton" }
        );
    }

    [MenuItem("Tools/Levels/Setup Level 4 (+ objective buttons)")]
    public static void SetupLevel4()
    {
        // Keep: Crate, PressurePlate, Lever, ElectricZone, ObjectiveButton.
        StripLevel(
            "Assets/Scenes/Level4.unity",
            stripScripts: new[] { "Portal", "PressButton", "RoboArm" },
            stripPrefabs: new[] { "SpringPlatform", "StandButton" }
        );
    }

    private static void StripLevel(string scenePath, string[] stripScripts, string[] stripPrefabs)
    {
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogError($"[LevelStripper] Scene not found: {scenePath}. Make sure you copied SampleScene.unity to {scenePath} first.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var toRemove = new HashSet<GameObject>();
        var scriptSet = new HashSet<string>(stripScripts);
        var prefabSet = new HashSet<string>(stripPrefabs);

        foreach (var root in scene.GetRootGameObjects())
        {
            CollectTargets(root.transform, scriptSet, prefabSet, toRemove);
        }

        int destroyed = 0;
        foreach (var go in toRemove)
        {
            if (go == null) continue;
            Debug.Log($"[LevelStripper] Removing: {GetHierarchyPath(go)}");
            Object.DestroyImmediate(go);
            destroyed++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[LevelStripper] DONE. Scene: {scenePath} | Removed: {destroyed} GameObjects.");
        EditorUtility.DisplayDialog(
            "Level Stripper",
            $"Stripped {destroyed} GameObjects from {System.IO.Path.GetFileName(scenePath)}.\n\nVerify the scene in Hierarchy. If something broke, restore from git.",
            "OK"
        );
    }

    private static void CollectTargets(Transform t, HashSet<string> scripts, HashSet<string> prefabs, HashSet<GameObject> bucket)
    {
        var go = t.gameObject;

        // Check by attached script type name
        foreach (var mb in go.GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            string typeName = mb.GetType().Name;
            if (scripts.Contains(typeName))
            {
                // Add the topmost prefab root if this is a prefab instance, otherwise this go itself
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                bucket.Add(root != null ? root : go);
                break;
            }
        }

        // Check by source prefab name
        if (PrefabUtility.IsPartOfPrefabInstance(go) && PrefabUtility.IsOutermostPrefabInstanceRoot(go))
        {
            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (prefabs.Contains(name))
            {
                bucket.Add(go);
            }
        }

        // Recurse — but make a snapshot since we might mutate later
        var children = new List<Transform>();
        for (int i = 0; i < t.childCount; i++) children.Add(t.GetChild(i));
        foreach (var c in children) CollectTargets(c, scripts, prefabs, bucket);
    }

    private static string GetHierarchyPath(GameObject go)
    {
        var path = go.name;
        var p = go.transform.parent;
        while (p != null)
        {
            path = p.name + "/" + path;
            p = p.parent;
        }
        return path;
    }
}
#endif
