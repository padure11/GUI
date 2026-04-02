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

    [ServerRpc(RequireOwnership = false)]
    public void SubmitCodeServerRpc(string code, int playerIndex, ServerRpcParams rpcParams = default)
    {
        Debug.Log("Player " + playerIndex + " submitted code");

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

        // Parsează ambele coduri în liste de comenzi
        List<Command> commands1 = parser.Parse(lexer.Tokenize(code1));
        List<Command> commands2 = parser.Parse(lexer.Tokenize(code2));

        // Flatten comenzile (inclusiv repeat) în liste simple
        List<string> flat1 = FlattenCommands(commands1);
        List<string> flat2 = FlattenCommands(commands2);

        int maxSteps = Mathf.Max(flat1.Count, flat2.Count);

        for (int i = 0; i < maxSteps; i++)
        {
            string cmd1 = i < flat1.Count ? flat1[i] : "none";
            string cmd2 = i < flat2.Count ? flat2[i] : "none";

            // Trimite comanda la toți clienții
            ExecuteStepClientRpc(cmd1, cmd2);

            // Așteaptă să termine ambii roboții
            yield return new WaitUntil(() => !robot1.IsMoving() && !robot2.IsMoving());
            yield return new WaitForSeconds(0.1f);
        }

        code1 = null;
        code2 = null;
        isExecuting = false;

        if (LevelManager.Instance != null)
            LevelManager.Instance.OnCodeFinished();
    }

    [ClientRpc]
    private void ExecuteStepClientRpc(string cmd1, string cmd2)
    {
        // Fiecare client execută local ambele comenzi
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
            case "wait":
                yield return new WaitForSeconds(0.5f);
                break;
            default:
                break;
        }
    }

    private List<string> FlattenCommands(List<Command> commands)
    {
        List<string> flat = new List<string>();

        foreach (Command cmd in commands)
        {
            if (cmd.type == "repeat")
            {
                for (int i = 0; i < cmd.argument; i++)
                {
                    flat.AddRange(FlattenCommands(cmd.body));
                }
            }
            else
            {
                flat.Add(cmd.type);
            }
        }

        return flat;
    }
}