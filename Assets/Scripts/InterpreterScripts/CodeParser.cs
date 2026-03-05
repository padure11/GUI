using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CommandParser : MonoBehaviour
{
    public PlayerController player;

    public void RunCode(string code)
    {
        List<string> commands = ParseCode(code);
        StartCoroutine(ExecuteCommands(commands));
    }

    List<string> ParseCode(string code)
    {
        List<string> commands = new List<string>();
        string[] lines = code.Split('\n');
        
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                commands.Add(trimmed);
        }
        return commands;
    }

    IEnumerator ExecuteCommands(List<string> commands)
    {
        foreach (string cmd in commands)
        {
            yield return StartCoroutine(ExecuteCommand(cmd));
        }
    }

    IEnumerator ExecuteCommand(string cmd)
    {
        if (cmd.StartsWith("moveForward"))
        {
            int n = ParseArgument(cmd);
            yield return StartCoroutine(player.MoveForward(n));
        }
        else if (cmd.StartsWith("moveBack"))
        {
            int n = ParseArgument(cmd);
            yield return StartCoroutine(player.MoveBack(n));
        }
        else if (cmd == "turnLeft()")
        {
            yield return StartCoroutine(player.TurnLeft());
        }
        else if (cmd == "turnRight()")
        {
            yield return StartCoroutine(player.TurnRight());
        }
        else if (cmd.StartsWith("wait"))
        {
            int n = ParseArgument(cmd);
            yield return new WaitForSeconds(n);
        }
        else if (cmd == "jump()")
        {
            yield return StartCoroutine(player.Jump());
        } 
        else
        {
            Debug.LogWarning("Comandă necunoscută: " + cmd);
        }
    }

    int ParseArgument(string cmd)
    {
        int start = cmd.IndexOf('(') + 1;
        int end = cmd.IndexOf(')');
        string arg = cmd.Substring(start, end - start);
        return int.Parse(arg);
    }
}