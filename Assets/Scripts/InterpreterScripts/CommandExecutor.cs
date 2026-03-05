using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CommandExecutor : MonoBehaviour
{
    public PlayerController player;
    
    private Lexer lexer = new Lexer();
    private Parser parser = new Parser();
    private bool isRunning = false;
    
    public void RunCode(string code)
    {
        if (isRunning) return;

        List<Token> tokens = lexer.Tokenize(code);
        List<Command> commands = parser.Parse(tokens);
        StartCoroutine(Execute(commands));
    }

    private IEnumerator Execute(List<Command> commands)
    {
        isRunning = true;

        foreach (Command cmd in commands)
        {
            yield return StartCoroutine(ExecuteCommand(cmd));
        }

        isRunning = false;
    }

    private IEnumerator ExecuteCommand(Command cmd)
    {
        switch (cmd.type)
        {
            case "moveForward":
                yield return StartCoroutine(player.MoveForward(1));
                break;
            case "moveBack":
                yield return StartCoroutine(player.MoveBack(1));
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

            // case "grab":
            //     yield return StartCoroutine(player.Grab());
            //     break;
            // case "drop":
            //     yield return StartCoroutine(player.Drop());
            //     break;
            // case "push":
            //     yield return StartCoroutine(player.Push());
            //     break;
            // case "press":
            //     yield return StartCoroutine(player.Press());
            //     break;

            case "wait":
                yield return new WaitForSeconds(0.5f);
                break;

            case "repeat":
                for (int i = 0; i < cmd.argument; i++)
                {
                    yield return StartCoroutine(Execute(cmd.body));
                }
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
                Debug.LogWarning("Unknown command: " + cmd.type);
                break;
        }
    }

    // private bool CheckCondition(string condition)
    // {
    //     switch (condition)
    //     {
    //         case "obstacleAhead":
    //             return player.IsObstacleAhead();
    //         case "nothingAhead":
    //             return !player.IsObstacleAhead();
    //         case "doorClosed":
    //             return player.IsDoorClosed();
    //         case "doorOpen":
    //             return !player.IsDoorClosed();
    //         case "holding":
    //             return player.IsHolding();
    //         case "notHolding":
    //             return !player.IsHolding();
    //         case "atEdge":
    //             return player.IsAtEdge();
    //         default:
    //             Debug.LogWarning("Unknown condition: " + condition);
    //             return false;
    //     }
    // }
}