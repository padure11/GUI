using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    public AudioClip music;

    void Start()
    {
        if (AudioManager.Instance != null && music != null)
            AudioManager.Instance.PlayMusic(music);
    }
}
