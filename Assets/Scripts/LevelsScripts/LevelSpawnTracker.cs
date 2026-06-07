using System.Collections.Generic;
using UnityEngine;

public class LevelSpawnTracker : MonoBehaviour
{
    public static LevelSpawnTracker Instance;

    private readonly List<GameObject> spawned = new List<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject obj = Instantiate(prefab, position, rotation);
        if (Instance != null)
            Instance.spawned.Add(obj);
        return obj;
    }

    public void DestroyAllSpawned()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i] != null)
                Destroy(spawned[i]);
        }
        spawned.Clear();
    }
}
