using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [Header("Display")]
    public KeyCode toggleKey = KeyCode.F3;
    public bool startVisible = true;
    public int fontSize = 18;
    public Color textColor = new Color(1f, 1f, 0f);

    private float deltaTime = 0f;
    private bool visible;

    void Awake()
    {
        visible = startVisible;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        if (Input.GetKeyDown(toggleKey)) visible = !visible;
    }

    void OnGUI()
    {
        if (!visible) return;

        float ms = deltaTime * 1000f;
        float fps = 1f / deltaTime;

        var style = new GUIStyle();
        style.fontSize = fontSize;
        style.normal.textColor = textColor;
        style.fontStyle = FontStyle.Bold;

        var rect = new Rect(10, 10, 300, 60);
        GUI.Label(rect, $"{fps:0.} FPS  ({ms:0.0} ms)", style);
    }
}
