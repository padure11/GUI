using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Try : MonoBehaviour
{
    public GameObject objectToSpawn;
    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("PushableCrate")) {
            Debug.Log(other.name + " is on the floor tile");
            Instantiate(objectToSpawn, spawnPosition, Quaternion.Euler(spawnRotation));
        }
    }   
}
