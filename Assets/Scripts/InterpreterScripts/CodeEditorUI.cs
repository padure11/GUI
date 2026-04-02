using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

public class CodeEditorUI : MonoBehaviour
{
    private TextField codeInput;
    private Button runButton;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        codeInput = root.Q<TextField>("code-input");
        runButton = root.Q<Button>("run-button");

        runButton.clicked += OnRunPressed;
    }

    void OnRunPressed()
    {
        string code = codeInput.value;

        int playerIndex = NetworkManager.Singleton.IsHost ? 0 : 1;

        GameSessionManager.Instance.SubmitCodeServerRpc(code, playerIndex);
    }
}