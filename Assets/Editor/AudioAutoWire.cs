#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AudioAutoWire
{
    private const string AudioFolder = "Assets/Audio";

    private static readonly string[] AllScenes = new[]
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Level1.unity",
        "Assets/Scenes/Level2.unity",
        "Assets/Scenes/Level3.unity",
        "Assets/Scenes/Level4.unity",
        "Assets/Scenes/SampleScene.unity",
    };

    // Slot field name on AudioManager → audio clip filename (without extension)
    private static readonly Dictionary<string, string> SlotToClip = new Dictionary<string, string>
    {
        { "uiClick",         "sfx_button_press" },
        { "levelComplete",   "sfx_level_complete" },
        { "codeRun",         "sfx_level_reset" },
        { "walkStep",        "sfx_robot_walk" },
        { "jump",            "sfx_spring" },
        { "land",            "sfx_crate_land" },
        { "press",           "sfx_button_press" },
        { "pull",            "sfx_lever_toggle" },
        { "die",             "sfx_robot_die" },
        { "leverPull",       "sfx_lever_toggle" },
        { "buttonPress",     "sfx_button_press" },
        { "electricBuzz",    "sfx_electric_zap" },
    };

    private const string MusicClipName = "music_background";

    [MenuItem("Tools/Audio/Auto-Wire Clips")]
    public static void AutoWire()
    {
        var clips = LoadAllClips();
        if (clips.Count == 0)
        {
            EditorUtility.DisplayDialog("Audio Auto-Wire",
                $"No AudioClips found in {AudioFolder}. Drop your .wav files there first.", "OK");
            return;
        }

        int totalAudioManagers = 0;
        int totalSceneMusic = 0;
        int totalSlotsSet = 0;
        int totalMusicSet = 0;

        foreach (var scenePath in AllScenes)
        {
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"[AudioAutoWire] Scene missing: {scenePath} — skipped");
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool dirty = false;

            // AudioManager
            foreach (var am in Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None))
            {
                totalAudioManagers++;
                var so = new SerializedObject(am);
                foreach (var pair in SlotToClip)
                {
                    if (!clips.TryGetValue(pair.Value, out var clip)) continue;
                    var prop = so.FindProperty(pair.Key);
                    if (prop == null) continue;
                    prop.objectReferenceValue = clip;
                    totalSlotsSet++;
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(am);
                dirty = true;
            }

            // SceneMusic
            if (clips.TryGetValue(MusicClipName, out var musicClip))
            {
                foreach (var sm in Object.FindObjectsByType<SceneMusic>(FindObjectsSortMode.None))
                {
                    totalSceneMusic++;
                    var so = new SerializedObject(sm);
                    var prop = so.FindProperty("music");
                    if (prop != null)
                    {
                        prop.objectReferenceValue = musicClip;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(sm);
                        dirty = true;
                        totalMusicSet++;
                    }
                }
            }

            if (dirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[AudioAutoWire] Updated scene: {scenePath}");
            }
            else
            {
                Debug.Log($"[AudioAutoWire] No AudioManager or SceneMusic in {scenePath} — nothing to do");
            }
        }

        string summary =
            $"AudioManagers wired: {totalAudioManagers} (slots set: {totalSlotsSet})\n" +
            $"SceneMusic wired: {totalSceneMusic} (music set: {totalMusicSet})";
        Debug.Log("[AudioAutoWire] DONE.\n" + summary);
        EditorUtility.DisplayDialog("Audio Auto-Wire", summary, "OK");
    }

    private static Dictionary<string, AudioClip> LoadAllClips()
    {
        var map = new Dictionary<string, AudioClip>();
        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) continue;
            string name = Path.GetFileNameWithoutExtension(path);
            map[name] = clip;
        }
        return map;
    }
}
#endif
