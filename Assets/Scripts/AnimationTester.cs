using System.Collections;
using UnityEngine;

public class AnimationTester : MonoBehaviour
{
    public PlayerController player;
    public Animator animator;

    [Header("Sequencing")]
    [Range(0f, 1f)]
    public float pauseBetweenSteps = 0.2f;

    private string sequenceText = "moveForward\nturnLeft\nmoveForward\njump";
    private bool isRunning = false;
    private string lastAction = "(none)";
    private bool isMovingFlag = false;

    void Start()
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (player == null) player = GetComponentInChildren<PlayerController>();
        if (animator == null && player != null) animator = player.GetComponentInChildren<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (player == null)
            Debug.LogError("AnimationTester: drag a PlayerController in the Inspector or attach this on the same GameObject.");
        if (animator == null)
            Debug.LogError("AnimationTester: no Animator found.");
    }

    void OnGUI()
    {
        if (animator == null) return;
        GUI.skin.button.fontSize = 14;
        GUI.skin.label.fontSize = 13;
        GUI.skin.textField.fontSize = 13;

        const int colW = 220;
        const int gap = 6;
        int x = 16, y = 16;

        GUI.Label(new Rect(x, y, colW, 24), "TRIGGERS  (last: " + lastAction + ")");
        y += 28;

        if (GUI.Button(new Rect(x, y, colW, 36), isMovingFlag ? "Stop Walk" : "Start Walk"))
        {
            isMovingFlag = !isMovingFlag;
            animator.SetBool("IsMoving", isMovingFlag);
            lastAction = isMovingFlag ? "Walk" : "Idle";
        }
        y += 36 + gap;

        foreach (var t in new[] { "Jump", "Climb", "Push", "Press", "TurnLeft", "TurnRight" })
        {
            if (GUI.Button(new Rect(x, y, colW, 32), t))
            {
                animator.SetTrigger(t);
                lastAction = t;
            }
            y += 32 + gap;
        }

        y += 6;
        if (GUI.Button(new Rect(x, y, colW, 28), "Reset to Idle"))
        {
            isMovingFlag = false;
            animator.SetBool("IsMoving", false);
            animator.Rebind();
            lastAction = "Idle (reset)";
        }
        y += 32;

        if (GUI.Button(new Rect(x, y, colW, 28), "Reset Position") && player != null)
        {
            player.transform.position = Vector3.zero;
            player.transform.rotation = Quaternion.identity;
        }

        // Right column: sequence
        int sx = 16 + colW + 24, sy = 16, sw = 360;
        GUI.Label(new Rect(sx, sy, sw, 24), "SEQUENCE  (one command per line)");
        sy += 28;
        sequenceText = GUI.TextArea(new Rect(sx, sy, sw, 200), sequenceText);
        sy += 208;

        GUI.Label(new Rect(sx, sy, sw, 22), "Pause between steps: " + pauseBetweenSteps.ToString("F2") + "s");
        sy += 22;
        pauseBetweenSteps = GUI.HorizontalSlider(new Rect(sx, sy, sw, 18), pauseBetweenSteps, 0f, 1f);
        sy += 28;

        GUI.enabled = !isRunning && player != null;
        if (GUI.Button(new Rect(sx, sy, sw, 36), isRunning ? "Running..." : "▶ Run Sequence"))
        {
            StartCoroutine(RunSequence(sequenceText));
        }
        GUI.enabled = true;
        sy += 44;

        GUI.Label(new Rect(sx, sy, sw, 18), "Available: moveForward, moveBack,");
        sy += 18;
        GUI.Label(new Rect(sx, sy, sw, 18), "turnLeft, turnRight, jump,");
        sy += 18;
        GUI.Label(new Rect(sx, sy, sw, 18), "push, press, wait");
    }

    IEnumerator RunSequence(string text)
    {
        if (isRunning || player == null) yield break;
        isRunning = true;

        var lines = text.Split('\n');
        foreach (var raw in lines)
        {
            string cmd = raw.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(cmd) || cmd.StartsWith("//") || cmd.StartsWith("#")) continue;

            lastAction = cmd;
            yield return StartCoroutine(RunCommand(cmd));
            if (pauseBetweenSteps > 0f)
                yield return new WaitForSeconds(pauseBetweenSteps);
        }

        lastAction = "(done)";
        isRunning = false;
    }

    IEnumerator RunCommand(string cmd)
    {
        switch (cmd)
        {
            case "moveforward": yield return player.MoveForward(); break;
            case "moveback":    yield return player.MoveBack(); break;
            case "turnleft":    yield return player.TurnLeft(); break;
            case "turnright":   yield return player.TurnRight(); break;
            case "jump":        yield return player.Jump(); break;
            case "climb":       yield return player.Jump(); break;
            case "push":        yield return player.Push(); break;
            case "press":       yield return player.Press(); break;
            case "wait":        yield return new WaitForSeconds(0.5f); break;
            default:            Debug.LogWarning("Unknown command: " + cmd); break;
        }
    }
}
