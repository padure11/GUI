using System.Collections.Generic;

public class Parser
{
    private List<Token> tokens;
    private int pos;

    public List<Command> Parse(List<Token> tokens)
    {
        this.tokens = tokens;
        this.pos = 0;
        return ParseBlock(false);
    }

    private List<Command> ParseBlock(bool insideBraces)
    {
        List<Command> commands = new List<Command>();

        while (pos < tokens.Count)
        {
            SkipNewlines();

            if (pos >= tokens.Count) break;

            Token current = tokens[pos];

            if (current.type == TokenType.EOF) break;
            if (current.type == TokenType.RBRACE)
            {
                if (insideBraces) { pos++; break; }
                else break;
            }

            Command cmd = ParseCommand();
            if (cmd != null) commands.Add(cmd);
        }

        return commands;
    }

    private Command ParseCommand()
    {
        Token current = tokens[pos];

        if (current.type == TokenType.COMMAND)
        {
            return ParseSimpleCommand();
        }

        if (current.type == TokenType.KEYWORD)
        {
            switch (current.value)
            {
                case "repeat": return ParseRepeat();
                case "while": return ParseWhile();
                case "if": return ParseIf();
                case "function": return ParseFunctionDef();
            }
        }

        pos++;
        return null;
    }

    private Command ParseFunctionDef()
    {
        Command cmd = new Command("function_def");
        pos++; // consume 'function'

        SkipNewlines();
        if (pos < tokens.Count && tokens[pos].type == TokenType.COMMAND)
        {
            cmd.name = tokens[pos].value;
            pos++;
        }
        else
        {
            UnityEngine.Debug.LogWarning("Parser: 'function' must be followed by a name");
            return cmd;
        }

        // Optional parens after function name: function name() { ... }
        if (pos < tokens.Count && tokens[pos].type == TokenType.LPAREN)
        {
            pos++;
            if (pos < tokens.Count && tokens[pos].type == TokenType.RPAREN) pos++;
        }

        SkipNewlines();
        if (pos < tokens.Count && tokens[pos].type == TokenType.LBRACE)
        {
            pos++;
            cmd.body = ParseBlock(true);
        }

        if (Lexer.IsBuiltinCommand(cmd.name))
            UnityEngine.Debug.LogError("Parser: function name '" + cmd.name + "' shadows a built-in command");

        return cmd;
    }

    private Command ParseSimpleCommand()
    {
        Command cmd = new Command(tokens[pos].value);
        pos++;

        if (pos < tokens.Count && tokens[pos].type == TokenType.LPAREN)
        {
            pos++;

            if (pos < tokens.Count && tokens[pos].type == TokenType.NUMBER)
            {
                cmd.argument = int.Parse(tokens[pos].value);
                pos++;
            }

            if (pos < tokens.Count && tokens[pos].type == TokenType.RPAREN)
                pos++;
        }

        return cmd;
    }

    private Command ParseRepeat()
    {
        Command cmd = new Command("repeat");
        pos++;

        if (pos < tokens.Count && tokens[pos].type == TokenType.LPAREN)
        {
            pos++;
            if (pos < tokens.Count && tokens[pos].type == TokenType.NUMBER)
            {
                cmd.argument = int.Parse(tokens[pos].value);
                pos++;
            }
            if (pos < tokens.Count && tokens[pos].type == TokenType.RPAREN)
                pos++;
        }

        SkipNewlines();
        if (pos < tokens.Count && tokens[pos].type == TokenType.LBRACE)
        {
            pos++;
            cmd.body = ParseBlock(true);
        }

        return cmd;
    }

    private Command ParseWhile()
    {
        Command cmd = new Command("while");
        pos++;

        if (pos < tokens.Count && tokens[pos].type == TokenType.LPAREN)
        {
            pos++;
            if (pos < tokens.Count && tokens[pos].type == TokenType.CONDITION)
            {
                cmd.condition = tokens[pos].value;
                pos++;
            }
            if (pos < tokens.Count && tokens[pos].type == TokenType.RPAREN)
                pos++;
        }

        SkipNewlines();
        if (pos < tokens.Count && tokens[pos].type == TokenType.LBRACE)
        {
            pos++;
            cmd.body = ParseBlock(true);
        }

        return cmd;
    }

    private Command ParseIf()
    {
        Command cmd = new Command("if");
        pos++;

        if (pos < tokens.Count && tokens[pos].type == TokenType.LPAREN)
        {
            pos++;
            if (pos < tokens.Count && tokens[pos].type == TokenType.CONDITION)
            {
                cmd.condition = tokens[pos].value;
                pos++;
            }
            if (pos < tokens.Count && tokens[pos].type == TokenType.RPAREN)
                pos++;
        }

        SkipNewlines();
        if (pos < tokens.Count && tokens[pos].type == TokenType.LBRACE)
        {
            pos++;
            cmd.body = ParseBlock(true);
        }

        SkipNewlines();
        if (pos < tokens.Count && tokens[pos].type == TokenType.KEYWORD 
            && tokens[pos].value == "else")
        {
            pos++;
            SkipNewlines();
            if (pos < tokens.Count && tokens[pos].type == TokenType.LBRACE)
            {
                pos++;
                cmd.elseBody = ParseBlock(true);
            }
        }

        return cmd;
    }

    private void SkipNewlines()
    {
        while (pos < tokens.Count && tokens[pos].type == TokenType.NEWLINE)
            pos++;
    }
}