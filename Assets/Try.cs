using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Try : MonoBehaviour
{
    public GameObject objectToSpawn;
    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    private bool hasSpawned = false;

    void OnTriggerEnter(Collider other) {
        if (hasSpawned) return;
        if (other.CompareTag("PushableCrate")) {
            Debug.Log(other.name + " is on the floor tile");
            LevelSpawnTracker.Spawn(objectToSpawn, spawnPosition, Quaternion.Euler(spawnRotation));
            hasSpawned = true;
        }
    }

    public void ResetSpawnState()
    {
        hasSpawned = false;
    }
}
