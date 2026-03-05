using UnityEngine;
using TMPro;

public class CodeEditorUI : MonoBehaviour
{
    public TMP_InputField codeInput;
    public CommandParser parser;

    public void OnRunPressed()
    {
        string code = codeInput.text;
        parser.RunCode(code);
    }
}
