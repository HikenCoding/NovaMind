using Microsoft.SemanticKernel;

public class HelpSkill 
{
    [KernelFunction] //this method is only used for the Agent NovaMind
    public string ShowHelp() //returning the text currently in our terminal
    {
        return "Available commands:\n" +
               "/help - Show this help message\n" +
               "Ask anything to interact with the AI model.";
    }
}