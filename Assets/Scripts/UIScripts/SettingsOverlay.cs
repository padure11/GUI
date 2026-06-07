using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Netcode;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(UIDocument))]
public class SettingsOverlay : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    private const string MenuSceneName = "MainMenu";

    private VisualElement overlay;
    private Button resumeButton;
    private Button leaveButton;
    private Button quitButton;
    private Slider musicSlider;
    private Slider sfxSlider;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        overlay = root.Q<VisualElement>("settings-overlay");
        resumeButton = root.Q<Button>("resume-button");
        leaveButton = root.Q<Button>("leave-button");
        quitButton = root.Q<Button>("quit-button");
        musicSlider = root.Q<Slider>("music-slider");
        sfxSlider = root.Q<Slider>("sfx-slider");

        resumeButton.clicked += Close;
        leaveButton.clicked += BackToMenu;
        quitButton.clicked += QuitGame;

        if (musicSlider != null)
        {
            musicSlider.value = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.5f;
            musicSlider.RegisterValueChangedCallback(OnMusicSliderChanged);
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 0.8f;
            sfxSlider.RegisterValueChangedCallback(OnSfxSliderChanged);
        }

        Close();
    }

    void OnDisable()
    {
        if (resumeButton != null) resumeButton.clicked -= Close;
        if (leaveButton != null) leaveButton.clicked -= BackToMenu;
        if (quitButton != null) quitButton.clicked -= QuitGame;
        if (musicSlider != null) musicSlider.UnregisterValueChangedCallback(OnMusicSliderChanged);
        if (sfxSlider != null) sfxSlider.UnregisterValueChangedCallback(OnSfxSliderChanged);
        IsOpen = false;
    }

    private void OnMusicSliderChanged(ChangeEvent<float> evt)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(evt.newValue);
    }

    private void OnSfxSliderChanged(ChangeEvent<float> evt)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(evt.newValue);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Toggle();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (overlay == null) return;
        overlay.style.display = DisplayStyle.Flex;
        IsOpen = true;
    }

    public void Close()
    {
        if (overlay == null) return;
        overlay.style.display = DisplayStyle.None;
        IsOpen = false;
    }

    private void BackToMenu()
    {
        Close();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(MenuSceneName);
    }

    private void QuitGame()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
