public enum TokenType
{
    COMMAND,     // moveForward, grab, push, etc
    KEYWORD,     // repeat, while, if, else
    CONDITION,   // obstacleAhead, doorClosed, etc
    NUMBER,      // 3, 5, 10
    LPAREN,      // (
    RPAREN,      // )
    LBRACE,      // {
    RBRACE,      // }
    NEWLINE,
    EOF
}

public class Token
{
    public TokenType type;
    public string value;

    public Token(TokenType type, string value)
    {
        this.type = type;
        this.value = value;
    }
}