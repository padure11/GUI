using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Netcode;

[RequireComponent(typeof(UIDocument))]
public class LevelCompleteUI : MonoBehaviour
{
    public static LevelCompleteUI Instance;

    private VisualElement root;
    private Button nextButton;
    private Button menuButton;
    private Label waitingLabel;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;

        root = doc.rootVisualElement.Q<VisualElement>("lc-root");
        nextButton = doc.rootVisualElement.Q<Button>("lc-next");
        menuButton = doc.rootVisualElement.Q<Button>("lc-menu");
        waitingLabel = doc.rootVisualElement.Q<Label>("lc-waiting");

        if (nextButton != null) nextButton.clicked += OnNextClicked;
        if (menuButton != null) menuButton.clicked += OnMenuClicked;

        Hide();
    }

    void OnDisable()
    {
        if (nextButton != null) nextButton.clicked -= OnNextClicked;
        if (menuButton != null) menuButton.clicked -= OnMenuClicked;
    }

    public void Hide()
    {
        if (root != null) root.style.display = DisplayStyle.None;
    }

    public void Show()
    {
        if (root == null) return;
        root.style.display = DisplayStyle.Flex;

        int activeIndex = SceneManager.GetActiveScene().buildIndex;
        int lastIndex = SceneManager.sceneCountInBuildSettings - 1;
        bool isLast = activeIndex >= lastIndex;

        if (nextButton != null)
            nextButton.style.display = isLast ? DisplayStyle.None : DisplayStyle.Flex;

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        if (nextButton != null) nextButton.SetEnabled(isHost);
        if (menuButton != null) menuButton.SetEnabled(isHost);
        if (waitingLabel != null)
            waitingLabel.style.display = isHost ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void OnNextClicked()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost) return;
        if (LevelManager.Instance == null) return;
        LevelManager.Instance.LoadNextLevel();
    }

    private void OnMenuClicked()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost) return;
        if (GameSessionManager.Instance != null)
            GameSessionManager.Instance.BackToMenuClientRpc();
    }
}
