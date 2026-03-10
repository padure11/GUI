using UnityEngine;
using UnityEngine.UIElements;

public class RTSCameraFocus : MonoBehaviour
{
    private VisualElement editorPanel;
    public static bool IsPointerOverUI { get; private set; }

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        editorPanel = root.Q<VisualElement>(className: "editor-panel");

        if (editorPanel == null) return;

        editorPanel.RegisterCallback<PointerEnterEvent>(evt => IsPointerOverUI = true);
        editorPanel.RegisterCallback<PointerLeaveEvent>(evt => IsPointerOverUI = false);
        
        editorPanel.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
        editorPanel.RegisterCallback<PointerMoveEvent>(evt => evt.StopPropagation());
        editorPanel.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
    }

    void OnDisable()
    {
        IsPointerOverUI = false;
    }
}