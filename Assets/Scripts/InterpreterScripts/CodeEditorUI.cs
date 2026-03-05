using UnityEngine;
using TMPro;

public class CodeEditorUI : MonoBehaviour
{
    public TMP_InputField codeInput;
    public CommandExecutor executor;

    public void OnRunPressed()
    {
        string code = codeInput.text;
        executor.RunCode(code);
    }
}