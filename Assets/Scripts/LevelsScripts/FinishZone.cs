using UnityEngine;

public class FinishZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Level Complete!");
            LevelManager.Instance.CompleteLevel();
        }
    }
}