using Microsoft.SemanticKernel;

public class HelpSkill 
{
    [KernelFunction]
    public string ShowHelp() //returning the text currently in our terminal
    {
        return "Available commands:\n" +
               "/help - Show this help message\n" +
               "Ask anything to interact with the AI model.";
    }
}