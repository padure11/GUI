using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CommandExecutor : MonoBehaviour
{
    public PlayerController player;
    public int maxCallDepth = 100;

    private Lexer lexer = new Lexer();
    private Parser parser = new Parser();
    private bool isRunning = false;
    private Dictionary<string, List<Command>> functions = new Dictionary<string, List<Command>>();
    private int callDepth = 0;

    public bool IsRunning()
    {
        return isRunning;
    }

    public void RunCode(string code)
    {
        if (isRunning) return;

        List<Token> tokens = lexer.Tokenize(code);
        List<Command> commands = parser.Parse(tokens);

        functions.Clear();
        callDepth = 0;
        List<Command> mainBody = new List<Command>();
        foreach (var cmd in commands)
        {
            if (cmd.type == "function_def" && !string.IsNullOrEmpty(cmd.name))
                functions[cmd.name] = cmd.body;
            else
                mainBody.Add(cmd);
        }

        StartCoroutine(Execute(mainBody, true));
    }

    private IEnumerator Execute(List<Command> commands, bool isRoot = false)
    {
        if (isRoot) {
            isRunning = true;
        }

        foreach (Command cmd in commands)
        {
            yield return StartCoroutine(ExecuteCommand(cmd));
        }

        if(isRoot) {
            isRunning = false;
        }
    }

    private IEnumerator ExecuteCommand(Command cmd)
    {
        
        while (player.IsMoving())
        {
            yield return null;
        }

        switch (cmd.type)
        {
            case "moveForward":
                yield return StartCoroutine(player.MoveForward());
                break;
            case "moveBack":
                yield return StartCoroutine(player.MoveBack());
                break;
            case "turnLeft":
                yield return StartCoroutine(player.TurnLeft());
                break;
            case "turnRight":
                yield return StartCoroutine(player.TurnRight());
                break;
            case "jump":
                yield return StartCoroutine(player.Jump());
                break;
            case "push":
                yield return StartCoroutine(player.Push());
                break;
            case "press":
                yield return StartCoroutine(player.Press());
                break;
            case "pull":
                yield return StartCoroutine(player.Pull());
                break;

            case "wait":
                yield return new WaitForSeconds(0.5f);
                break;

            case "repeat":
                for (int i = 0; i < cmd.argument; i++)
                {
                    yield return StartCoroutine(Execute(cmd.body, false));
                }
                break;

            case "function_def":
                break;

            // case "while":
            //     int iterations = 0;
            //     while (CheckCondition(cmd.condition) && iterations < maxWhileIterations)
            //     {
            //         yield return StartCoroutine(Execute(cmd.body));
            //         iterations++;
            //     }
            //     if (iterations >= maxWhileIterations)
            //         Debug.LogError("Infinite loop detected!");
            //     break;

            // case "if":
            //     if (CheckCondition(cmd.condition))
            //         yield return StartCoroutine(Execute(cmd.body));
            //     else if (cmd.elseBody.Count > 0)
            //         yield return StartCoroutine(Execute(cmd.elseBody));
            //     break;

            default:
                if (functions.TryGetValue(cmd.type, out var body))
                {
                    if (callDepth >= maxCallDepth)
                    {
                        Debug.LogError("Function recursion limit reached (" + maxCallDepth + ") at '" + cmd.type + "'");
                        break;
                    }
                    callDepth++;
                    yield return StartCoroutine(Execute(body, false));
                    callDepth--;
                }
                else
                {
                    Debug.LogWarning("Unknown command: " + cmd.type);
                }
                break;
        }
    }
}