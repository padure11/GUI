using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class GameSessionManager : NetworkBehaviour
{
    public static GameSessionManager Instance;

    public PlayerController robot1;
    public PlayerController robot2;

    private string code1 = null;
    private string code2 = null;
    private bool isExecuting = false;

    private Lexer lexer = new Lexer();
    private Parser parser = new Parser();

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (robot1 != null) robot1.playerIndex = 0;
        if (robot2 != null) robot2.playerIndex = 1;
        BindLocalCamera();
    }

    private void BindLocalCamera()
    {
        var rig = FindAnyObjectByType<RTSCamera>();
        if (rig == null) { Debug.LogWarning("[Camera] no RTSCamera found"); return; }

        rig.SetStartView(IsHost);

        PlayerController localRobot = IsHost ? robot1 : robot2;
        if (localRobot != null) rig.SetPivot(localRobot.transform);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitCodeServerRpc(string code, int playerIndex, ServerRpcParams rpcParams = default)
    {
        string label = playerIndex == 0 ? "<color=cyan>[P1]</color>" : "<color=orange>[P2]</color>";
        int lines = code != null ? code.Split('\n').Length : 0;
        Debug.Log($"{label} Submitted code ({lines} lines)");

        if (playerIndex == 0)
            code1 = code;
        else
            code2 = code;

        if (code1 != null && code2 != null && !isExecuting)
        {
            StartCoroutine(ExecuteBothStepByStep());
        }
    }

    private IEnumerator ExecuteBothStepByStep()
    {
        isExecuting = true;

        List<Command> raw1 = parser.Parse(lexer.Tokenize(code1));
        List<Command> raw2 = parser.Parse(lexer.Tokenize(code2));

        var funcs1 = new Dictionary<string, List<Command>>();
        var funcs2 = new Dictionary<string, List<Command>>();
        List<Command> commands1 = ExtractFunctions(raw1, funcs1);
        List<Command> commands2 = ExtractFunctions(raw2, funcs2);

        List<string> flat1 = FlattenCommands(commands1, funcs1, 0);
        List<string> flat2 = FlattenCommands(commands2, funcs2, 0);

        int maxSteps = Mathf.Max(flat1.Count, flat2.Count);

        Debug.Log($"<b>=== Run started ===</b> ({maxSteps} steps total)");

        for (int i = 0; i < maxSteps; i++)
        {
            string cmd1 = i < flat1.Count ? flat1[i] : "none";
            string cmd2 = i < flat2.Count ? flat2[i] : "none";

            string c1 = FormatCmdForLog(cmd1, robot1);
            string c2 = FormatCmdForLog(cmd2, robot2);
            Debug.Log($"<b>[Step {i + 1}]</b> P1: {c1} | P2: {c2}");

            ExecuteStep(cmd1, cmd2);

            // Așteaptă puțin ca ClientRpc să ajungă și să înceapă execuția
            yield return new WaitForSeconds(0.2f);
            yield return new WaitUntil(() => !robot1.IsMoving() && !robot2.IsMoving());
            yield return new WaitForSeconds(0.1f);
        }

        code1 = null;
        code2 = null;
        isExecuting = false;

        bool r1Finish = robot1 != null && robot1.IsOnFinish();
        bool r2Finish = robot2 != null && robot2.IsOnFinish();
        bool buttonsOk = ObjectiveButton.AllPressed();
        bool completed = r1Finish && r2Finish && buttonsOk;
        Debug.Log($"<b>[Completion]</b> robot1 on finish: {r1Finish} | robot2 on finish: {r2Finish} | all buttons pressed: {buttonsOk}");

        if (completed && LevelManager.Instance != null)
            LevelManager.Instance.CompleteLevel();

        if (!completed)
        {
            Debug.Log("<b>=== Run end ===</b> Level not completed, resetting all");
            ResetLevelClientRpc();
        }
        else
        {
            Debug.Log("<b>=== Run end ===</b> <color=lime>Level completed!</color>");
            ExecutionFinishedClientRpc();
        }
    }

    private string FormatCmdForLog(string cmd, PlayerController robot)
    {
        if (cmd == "none") return "<color=grey>—</color>";
        if (robot != null && robot.IsDead()) return $"<color=grey>{cmd} (dead)</color>";
        return cmd;
    }

    [ClientRpc]
    private void ResetLevelClientRpc()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.ResetLevelLocal();
        NotifyEditorIdle();
    }

    [ClientRpc]
    private void ExecutionFinishedClientRpc()
    {
        NotifyEditorIdle();
    }

    [ClientRpc]
    public void ShowLevelCompleteClientRpc()
    {
        if (LevelCompleteUI.Instance != null) LevelCompleteUI.Instance.Show();
    }

    [ClientRpc]
    public void BackToMenuClientRpc()
    {
        StartCoroutine(BackToMenuRoutine());
    }

    private System.Collections.IEnumerator BackToMenuRoutine()
    {
        yield return null;
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        yield return null;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private void NotifyEditorIdle()
    {
        var editor = FindAnyObjectByType<CodeEditorUI>();
        if (editor != null) editor.NotifyExecutionFinished();
    }

    private void ExecuteStep(string cmd1, string cmd2)
    {
        if (!IsServer) return;
        if (cmd1 != "none")
            StartCoroutine(ExecuteSingleCommand(robot1, cmd1));
        if (cmd2 != "none")
            StartCoroutine(ExecuteSingleCommand(robot2, cmd2));
    }

    private IEnumerator ExecuteSingleCommand(PlayerController robot, string cmd)
    {
        switch (cmd)
        {
            case "moveForward":
                yield return StartCoroutine(robot.MoveForward());
                break;
            case "moveBack":
                yield return StartCoroutine(robot.MoveBack());
                break;
            case "turnLeft":
                yield return StartCoroutine(robot.TurnLeft());
                break;
            case "turnRight":
                yield return StartCoroutine(robot.TurnRight());
                break;
            case "jump":
                yield return StartCoroutine(robot.Jump());
                break;
            case "push":
                yield return StartCoroutine(robot.Push());
                break;
            case "press":
                yield return StartCoroutine(robot.Press());
                break;
            case "pull":
                yield return StartCoroutine(robot.Pull());
                break;
            case "wait":
                yield return new WaitForSeconds(0.5f);
                break;
            default:
                break;
        }
    }

    private const int MaxFunctionDepth = 100;

    private List<Command> ExtractFunctions(List<Command> raw, Dictionary<string, List<Command>> funcs)
    {
        var main = new List<Command>();
        foreach (var cmd in raw)
        {
            if (cmd.type == "function_def" && !string.IsNullOrEmpty(cmd.name))
                funcs[cmd.name] = cmd.body;
            else
                main.Add(cmd);
        }
        return main;
    }

    private List<string> FlattenCommands(List<Command> commands, Dictionary<string, List<Command>> funcs, int depth)
    {
        List<string> flat = new List<string>();
        if (depth >= MaxFunctionDepth)
        {
            Debug.LogError("Function recursion limit reached (" + MaxFunctionDepth + ")");
            return flat;
        }

        foreach (Command cmd in commands)
        {
            if (cmd.type == "repeat")
            {
                for (int i = 0; i < cmd.argument; i++)
                    flat.AddRange(FlattenCommands(cmd.body, funcs, depth));
            }
            else if (cmd.type == "function_def")
            {
                continue;
            }
            else if (funcs.TryGetValue(cmd.type, out var body))
            {
                flat.AddRange(FlattenCommands(body, funcs, depth + 1));
            }
            else
            {
                flat.Add(cmd.type);
            }
        }

        return flat;
    }
}