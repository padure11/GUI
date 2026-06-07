// Lexer.cs
using System.Collections.Generic;

public class Lexer
{
    private static HashSet<string> commands = new HashSet<string>
    {
        "moveForward", "moveBack", "turnLeft", "turnRight",
        "jump", "grab", "drop", "push", "press", "pull", "wait"
    };

    private static HashSet<string> keywords = new HashSet<string>
    {
        "repeat", "while", "if", "else", "function"
    };

    public static bool IsBuiltinCommand(string name) => commands.Contains(name);

    private static HashSet<string> conditions = new HashSet<string>
    {
        "obstacleAhead", "nothingAhead", "doorClosed",
        "doorOpen", "holding", "notHolding", "atEdge"
    };

    public List<Token> Tokenize(string code)
    {
        List<Token> tokens = new List<Token>();
        int i = 0;

        while (i < code.Length)
        {
            char c = code[i];

            if (c == ' ' || c == '\t')
            {
                i++;
                continue;
            }

            if (c == '\n' || c == '\r')
            {
                tokens.Add(new Token(TokenType.NEWLINE, "\\n"));
                i++;
                if (i < code.Length && code[i] == '\n') i++;
                continue;
            }

            if (c == '(') { tokens.Add(new Token(TokenType.LPAREN, "(")); i++; continue; }
            if (c == ')') { tokens.Add(new Token(TokenType.RPAREN, ")")); i++; continue; }
            if (c == '{') { tokens.Add(new Token(TokenType.LBRACE, "{")); i++; continue; }
            if (c == '}') { tokens.Add(new Token(TokenType.RBRACE, "}")); i++; continue; }

            if (char.IsDigit(c))
            {
                string num = "";
                while (i < code.Length && char.IsDigit(code[i]))
                {
                    num += code[i];
                    i++;
                }
                tokens.Add(new Token(TokenType.NUMBER, num));
                continue;
            }

            if (char.IsLetter(c))
            {
                string word = "";
                while (i < code.Length && char.IsLetterOrDigit(code[i]))
                {
                    word += code[i];
                    i++;
                }

                if (commands.Contains(word))
                    tokens.Add(new Token(TokenType.COMMAND, word));
                else if (keywords.Contains(word))
                    tokens.Add(new Token(TokenType.KEYWORD, word));
                else if (conditions.Contains(word))
                    tokens.Add(new Token(TokenType.CONDITION, word));
                else
                    tokens.Add(new Token(TokenType.COMMAND, word));
                continue;
            }

            i++;
        }

        tokens.Add(new Token(TokenType.EOF, ""));
        return tokens;
    }
}