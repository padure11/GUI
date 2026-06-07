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
            ResetLevelLocal();
    }

    public void CompleteLevel()
    {
        levelCompleted = true;
        Debug.Log("Level Complete!");
        if (GameSessionManager.Instance != null)
            GameSessionManager.Instance.ShowLevelCompleteClientRpc();
    }

    public void ResetLevelLocal()
    {
        foreach (var trigger in FindObjectsByType<Try>(FindObjectsSortMode.None))
            trigger.ResetSpawnState();

        if (LevelSpawnTracker.Instance != null)
            LevelSpawnTracker.Instance.DestroyAllSpawned();

        bool isServer = NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer;
        if (isServer)
        {
            foreach (var resettable in FindObjectsByType<LevelResettable>(FindObjectsSortMode.None))
                resettable.ResetState();

            foreach (var lever in FindObjectsByType<Lever>(FindObjectsSortMode.None))
                lever.ResetToInitial();

            foreach (var btn in FindObjectsByType<ObjectiveButton>(FindObjectsSortMode.None))
                btn.ResetToInitial();

            foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                player.ResetToStart();
        }

        Debug.Log("Level reset (local).");
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

    public void LoadNextLevel()
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

    public bool IsCompleted()
    {
        return levelCompleted;
    }
}