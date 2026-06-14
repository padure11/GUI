using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Netcode;

[RequireComponent(typeof(UIDocument))]
public class CodeEditorUI : MonoBehaviour
{
    public static bool IsInteracting { get; private set; }
    public static bool IsPointerOverPanel { get; private set; }

    public static bool BlocksCamera => IsInteracting || IsPointerOverPanel || SettingsOverlay.IsOpen;

    private VisualElement panel;
    private VisualElement titleBar;
    private VisualElement resizeHandle;
    private VisualElement statusDot;
    private Label statusText;
    private TextField codeInput;
    private Button runButton;
    private Button minimizeButton;
    private Button closeButton;
    private Button helpButton;
    private VisualElement cheatsheet;

    private bool isMinimized = false;
    private bool isDragging = false;
    private bool isResizing = false;

    private Vector2 dragStartMouse;
    private Vector2 dragStartPanelPos;
    private Vector2 resizeStartMouse;
    private Vector2 resizeStartSize;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        panel = root.Q<VisualElement>("editor-panel");
        titleBar = root.Q<VisualElement>("title-bar");
        resizeHandle = root.Q<VisualElement>("resize-handle");
        statusDot = root.Q<VisualElement>("status-dot");
        statusText = root.Q<Label>("status-text");
        codeInput = root.Q<TextField>("code-input");
        runButton = root.Q<Button>("run-button");
        minimizeButton = root.Q<Button>("minimize-button");
        closeButton = root.Q<Button>("close-button");
        helpButton = root.Q<Button>("help-button");
        cheatsheet = root.Q<VisualElement>("cheatsheet");

        runButton.clicked += OnRunPressed;
        minimizeButton.clicked += ToggleMinimize;
        closeButton.clicked += ClosePanel;
        if (helpButton != null) helpButton.clicked += ToggleCheatsheet;

        titleBar.RegisterCallback<PointerDownEvent>(OnDragStart);
        titleBar.RegisterCallback<PointerMoveEvent>(OnDragMove);
        titleBar.RegisterCallback<PointerUpEvent>(OnDragEnd);

        resizeHandle.RegisterCallback<PointerDownEvent>(OnResizeStart);
        resizeHandle.RegisterCallback<PointerMoveEvent>(OnResizeMove);
        resizeHandle.RegisterCallback<PointerUpEvent>(OnResizeEnd);

        codeInput.RegisterCallback<FocusInEvent>(_ => IsInteracting = true);
        codeInput.RegisterCallback<FocusOutEvent>(_ =>
        {
            if (!isDragging && !isResizing) IsInteracting = false;
        });

        panel.RegisterCallback<PointerEnterEvent>(_ => IsPointerOverPanel = true);
        panel.RegisterCallback<PointerLeaveEvent>(_ => IsPointerOverPanel = false);

        ApplyLevelClass();

        SetStatus(false);
    }

    private void ApplyLevelClass()
    {
        if (cheatsheet == null) return;
        int idx = SceneManager.GetActiveScene().buildIndex;
        for (int i = 1; i <= 5; i++) cheatsheet.RemoveFromClassList($"level-{i}");
        cheatsheet.AddToClassList($"level-{idx}");
    }

    void OnDisable()
    {
        if (runButton != null) runButton.clicked -= OnRunPressed;
        if (minimizeButton != null) minimizeButton.clicked -= ToggleMinimize;
        if (closeButton != null) closeButton.clicked -= ClosePanel;
        if (helpButton != null) helpButton.clicked -= ToggleCheatsheet;
        IsInteracting = false;
        IsPointerOverPanel = false;
    }

    private void ToggleCheatsheet()
    {
        if (cheatsheet == null) return;
        bool visible = cheatsheet.resolvedStyle.display != DisplayStyle.None;
        cheatsheet.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1) && panel != null)
            panel.style.display = DisplayStyle.Flex;

        if (Input.GetMouseButtonDown(0) && !IsPointerOverPanel && codeInput != null)
        {
            var fc = codeInput.focusController;
            if (fc != null && fc.focusedElement != null)
            {
                (fc.focusedElement as VisualElement)?.Blur();
                IsInteracting = false;
            }
        }
    }

    void OnRunPressed()
    {
        if (NetworkManager.Singleton == null || GameSessionManager.Instance == null) return;

        string code = codeInput.value;
        int playerIndex = NetworkManager.Singleton.IsHost ? 0 : 1;

        SetStatus(true);
        runButton.SetEnabled(false);
        GameSessionManager.Instance.SubmitCodeServerRpc(code, playerIndex);
    }

    public void NotifyExecutionFinished()
    {
        SetStatus(false);
        if (runButton != null) runButton.SetEnabled(true);
    }

    private void SetStatus(bool running)
    {
        if (statusDot == null || statusText == null) return;
        if (running)
        {
            statusDot.RemoveFromClassList("status-dot-idle");
            statusDot.AddToClassList("status-dot-running");
            statusText.text = "running";
        }
        else
        {
            statusDot.RemoveFromClassList("status-dot-running");
            statusDot.AddToClassList("status-dot-idle");
            statusText.text = "idle";
        }
    }

    private void ToggleMinimize()
    {
        isMinimized = !isMinimized;
        if (isMinimized)
        {
            panel.AddToClassList("minimized");
            minimizeButton.text = "□";
        }
        else
        {
            panel.RemoveFromClassList("minimized");
            minimizeButton.text = "—";
        }
    }

    private void ClosePanel()
    {
        panel.style.display = DisplayStyle.None;
        IsInteracting = false;
    }

    private void OnDragStart(PointerDownEvent evt)
    {
        if (evt.button != 0) return;
        if (evt.target is Button) return;

        isDragging = true;
        dragStartMouse = evt.position;
        dragStartPanelPos = new Vector2(panel.resolvedStyle.left, panel.resolvedStyle.top);
        titleBar.CapturePointer(evt.pointerId);
        IsInteracting = true;
        evt.StopPropagation();
    }

    private void OnDragMove(PointerMoveEvent evt)
    {
        if (!isDragging) return;
        Vector2 delta = (Vector2)evt.position - dragStartMouse;
        Vector2 newPos = dragStartPanelPos + delta;

        float maxX = panel.parent.resolvedStyle.width - panel.resolvedStyle.width;
        float maxY = panel.parent.resolvedStyle.height - panel.resolvedStyle.height;
        newPos.x = Mathf.Clamp(newPos.x, 0, Mathf.Max(0, maxX));
        newPos.y = Mathf.Clamp(newPos.y, 0, Mathf.Max(0, maxY));

        panel.style.left = newPos.x;
        panel.style.top = newPos.y;
        panel.style.right = StyleKeyword.Auto;
        panel.style.bottom = StyleKeyword.Auto;
    }

    private void OnDragEnd(PointerUpEvent evt)
    {
        if (!isDragging) return;
        isDragging = false;
        titleBar.ReleasePointer(evt.pointerId);
        RefreshInteracting();
        evt.StopPropagation();
    }

    private void OnResizeStart(PointerDownEvent evt)
    {
        if (evt.button != 0 || isMinimized) return;

        isResizing = true;
        resizeStartMouse = evt.position;
        resizeStartSize = new Vector2(panel.resolvedStyle.width, panel.resolvedStyle.height);
        resizeHandle.CapturePointer(evt.pointerId);
        IsInteracting = true;
        evt.StopPropagation();
    }

    private void OnResizeMove(PointerMoveEvent evt)
    {
        if (!isResizing) return;
        Vector2 delta = (Vector2)evt.position - resizeStartMouse;
        float newW = Mathf.Max(280f, resizeStartSize.x + delta.x);
        float newH = Mathf.Max(180f, resizeStartSize.y + delta.y);
        panel.style.width = newW;
        panel.style.height = newH;
    }

    private void OnResizeEnd(PointerUpEvent evt)
    {
        if (!isResizing) return;
        isResizing = false;
        resizeHandle.ReleasePointer(evt.pointerId);
        RefreshInteracting();
        evt.StopPropagation();
    }

    private void RefreshInteracting()
    {
        IsInteracting = isDragging || isResizing || (codeInput != null && codeInput.focusController != null && codeInput.focusController.focusedElement == codeInput);
    }
}
