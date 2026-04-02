using UnityEngine;
using Unity.Netcode;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    private bool levelCompleted = false;

    void Awake()
    {
        Instance = this;
        Debug.Log("LevelManager initialized");
    }

    public void OnCodeFinished()
    {
        if (!levelCompleted)
            ResetLevel();
    }

    public void CompleteLevel()
    {
        levelCompleted = true;
        Debug.Log("Level Complete!");
        Invoke("LoadNextLevel", 2f);
    }

    public void ResetLevel()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private void LoadNextLevel()
    {
        int nextIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(
                    UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(nextIndex));
                NetworkManager.Singleton.SceneManager.LoadScene(sceneName,
                    UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }
        else
        {
            Debug.Log("Ai terminat toate nivelele!");
        }
    }
}