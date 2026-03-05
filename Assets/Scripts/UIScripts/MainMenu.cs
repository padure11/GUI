using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void QuitButton() {
        Application.Quit();
        Debug.Log("Game Closed!");
    }

    public void PlayButton() {
        SceneManager.LoadScene("SampleScene");
        Debug.Log("Game Started!");
    }
}
