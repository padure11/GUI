using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    private bool levelCompleted = false;

    void Awake()
    {
        Instance = this;
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void LoadNextLevel()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextIndex);
        else
            Debug.Log("Ai terminat toate nivelele!");
    }
}