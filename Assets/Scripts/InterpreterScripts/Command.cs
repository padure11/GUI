using System.Collections.Generic;

public class Command
{
    public string type;
    public int argument;
    public string condition;
    public string name;
    public List<Command> body;
    public List<Command> elseBody;

    public Command(string type)
    {
        this.type = type;
        this.argument = 0;
        this.condition = "";
        this.name = "";
        this.body = new List<Command>();
        this.elseBody = new List<Command>();
    }
}