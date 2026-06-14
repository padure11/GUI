using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(UIDocument))]
public class MainMenu : MonoBehaviour
{
    private const string GameSceneName = "Level1";
    private static readonly Regex JoinCodePattern = new Regex("^[A-Z0-9]{6}$");

    private Button hostButton;
    private Button joinButton;
    private Button quitButton;
    private Button copyCodeButton;
    private Button settingsButton;
    private Button settingsCloseButton;
    private TextField joinCodeInput;
    private Label joinCodeDisplay;
    private Label statusLabel;
    private VisualElement settingsPanel;
    private Slider musicSlider;
    private Slider sfxSlider;

    private string currentJoinCode = "";

    private bool isInitialized = false;
    private bool isHostWaiting = false;
    private bool callbacksHooked = false;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        hostButton = root.Q<Button>("host-button");
        joinButton = root.Q<Button>("join-button");
        quitButton = root.Q<Button>("quit-button");
        copyCodeButton = root.Q<Button>("copy-code-button");
        settingsButton = root.Q<Button>("settings-button");
        settingsCloseButton = root.Q<Button>("settings-close-button");
        joinCodeInput = root.Q<TextField>("join-code-input");
        joinCodeDisplay = root.Q<Label>("join-code-display");
        statusLabel = root.Q<Label>("status-label");
        settingsPanel = root.Q<VisualElement>("settings-panel");
        musicSlider = root.Q<Slider>("music-slider");
        sfxSlider = root.Q<Slider>("sfx-slider");

        hostButton.clicked += OnHostClicked;
        joinButton.clicked += OnJoinClicked;
        quitButton.clicked += OnQuitClicked;
        if (copyCodeButton != null) copyCodeButton.clicked += OnCopyCodeClicked;
        if (settingsButton != null) settingsButton.clicked += OpenSettings;
        if (settingsCloseButton != null) settingsCloseButton.clicked += CloseSettings;

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

        joinCodeInput.RegisterValueChangedCallback(OnJoinCodeChanged);

        CloseSettings();
        SetButtonsEnabled(false);
        SetCopyButtonVisible(false);
    }

    void OnDisable()
    {
        if (hostButton != null) hostButton.clicked -= OnHostClicked;
        if (joinButton != null) joinButton.clicked -= OnJoinClicked;
        if (quitButton != null) quitButton.clicked -= OnQuitClicked;
        if (copyCodeButton != null) copyCodeButton.clicked -= OnCopyCodeClicked;
        if (settingsButton != null) settingsButton.clicked -= OpenSettings;
        if (settingsCloseButton != null) settingsCloseButton.clicked -= CloseSettings;
        if (musicSlider != null) musicSlider.UnregisterValueChangedCallback(OnMusicSliderChanged);
        if (sfxSlider != null) sfxSlider.UnregisterValueChangedCallback(OnSfxSliderChanged);
        if (joinCodeInput != null) joinCodeInput.UnregisterValueChangedCallback(OnJoinCodeChanged);

        UnhookNetworkCallbacks();
    }

    private void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.style.display = DisplayStyle.Flex;
    }

    private void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.style.display = DisplayStyle.None;
    }

    private void OnMusicSliderChanged(ChangeEvent<float> evt)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(evt.newValue);
    }

    private void OnSfxSliderChanged(ChangeEvent<float> evt)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(evt.newValue);
    }

    async void Start()
    {
        Application.runInBackground = true;
        ApplyPerformanceSettings();

        if (NetworkManager.Singleton != null)
            DontDestroyOnLoad(NetworkManager.Singleton.gameObject);

        try
        {
            if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            isInitialized = true;
            SetButtonsEnabled(true);
            SetStatus("");
        }
        catch (System.Exception e)
        {
            SetStatus("Init error: " + e.Message);
            Debug.LogError(e);
        }
    }

    async void OnHostClicked()
    {
        if (!isInitialized) return;
        SetButtonsEnabled(false);
        SetStatus("Creating relay...");

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            HookNetworkCallbacks();

            if (!NetworkManager.Singleton.StartHost())
            {
                SetStatus("Failed to start host");
                SetButtonsEnabled(true);
                UnhookNetworkCallbacks();
                return;
            }

            isHostWaiting = true;
            currentJoinCode = code;
            joinCodeDisplay.text = "Code: " + code;
            SetCopyButtonVisible(true);
            SetStatus("Waiting for player 2...");
        }
        catch (System.Exception e)
        {
            SetStatus("Host error: " + e.Message);
            SetButtonsEnabled(true);
            Debug.LogError(e);
        }
    }

    async void OnJoinClicked()
    {
        if (!isInitialized) return;

        string code = (joinCodeInput.value ?? "").Trim().ToUpperInvariant();
        if (!JoinCodePattern.IsMatch(code))
        {
            SetStatus("Invalid code (need 6 letters/digits)");
            return;
        }

        SetButtonsEnabled(false);
        SetStatus("Joining " + code + "...");

        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(code);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );

            HookNetworkCallbacks();

            if (!NetworkManager.Singleton.StartClient())
            {
                SetStatus("Failed to start client");
                SetButtonsEnabled(true);
                UnhookNetworkCallbacks();
                return;
            }

            SetStatus("Connecting...");
        }
        catch (System.Exception e)
        {
            SetStatus("Join error: " + e.Message);
            SetButtonsEnabled(true);
            Debug.LogError(e);
        }
    }

    void OnCopyCodeClicked()
    {
        if (string.IsNullOrEmpty(currentJoinCode)) return;
        GUIUtility.systemCopyBuffer = currentJoinCode;
        SetStatus("Code copied to clipboard");
    }

    void OnJoinCodeChanged(ChangeEvent<string> evt)
    {
        string upper = (evt.newValue ?? "").ToUpperInvariant();
        if (upper != evt.newValue)
            joinCodeInput.SetValueWithoutNotify(upper);

        if (joinButton != null && isInitialized)
            joinButton.SetEnabled(JoinCodePattern.IsMatch(upper.Trim()));
    }

    private void ApplyPerformanceSettings()
    {
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
        QualitySettings.shadowDistance = 40f;
        QualitySettings.shadowResolution = ShadowResolution.Medium;
        QualitySettings.shadowCascades = 2;
        QualitySettings.shadowmaskMode = ShadowmaskMode.DistanceShadowmask;
        QualitySettings.skinWeights = SkinWeights.TwoBones;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
    }

    void OnQuitClicked()
    {
        Debug.Log("Game Closed!");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HookNetworkCallbacks()
    {
        if (callbacksHooked || NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        callbacksHooked = true;
    }

    private void UnhookNetworkCallbacks()
    {
        if (!callbacksHooked || NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        callbacksHooked = false;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
                SetStatus("Connected. Waiting for host...");
            return;
        }

        int count = NetworkManager.Singleton.ConnectedClientsList.Count;
        SetStatus("Players: " + count + "/2");

        if (isHostWaiting && count >= 2)
        {
            isHostWaiting = false;
            SetStatus("Loading game...");
            NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost && isHostWaiting)
        {
            int count = NetworkManager.Singleton.ConnectedClientsList.Count;
            SetStatus("Player left. Waiting (" + count + "/2)...");
        }
        else if (!NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
        {
            SetStatus("Disconnected from host");
            SetButtonsEnabled(true);
            UnhookNetworkCallbacks();
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        if (hostButton != null) hostButton.SetEnabled(enabled);
        if (joinButton != null)
        {
            string typed = (joinCodeInput != null ? joinCodeInput.value ?? "" : "").Trim().ToUpperInvariant();
            joinButton.SetEnabled(enabled && JoinCodePattern.IsMatch(typed));
        }
    }

    private void SetCopyButtonVisible(bool visible)
    {
        if (copyCodeButton != null)
            copyCodeButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetStatus(string message)
    {
        if (statusLabel != null) statusLabel.text = message;
    }
}
