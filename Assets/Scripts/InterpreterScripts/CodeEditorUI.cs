using UnityEngine;
using UnityEngine.UIElements;

public class CodeEditorUI : MonoBehaviour
{
    public CommandExecutor executor;
    
    private TextField codeInput;
    private Button runButton;
    private ScrollView consoleScroll;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        codeInput = root.Q<TextField>("code-input");
        runButton = root.Q<Button>("run-button");
        consoleScroll = root.Q<ScrollView>("console-scroll");

        runButton.clicked += OnRunPressed;
    }

    void OnRunPressed()
    {
        string code = codeInput.value;
        executor.RunCode(code);
    }
}