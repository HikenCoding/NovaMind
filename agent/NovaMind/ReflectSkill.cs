using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

// C# 12 Primary Constructor
public class ReflectSkill(IChatCompletionService chatCompletion)
{
    [KernelFunction]
    public async Task<string> Reflect(string input)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("Du bist ein Reflexionsmodul. Analysiere das Ergebnis und gib eine kurze Bewertung ab.");
        chatHistory.AddUserMessage($"Reflektiere dieses Ergebnis:\n\n{input}");

        // Nutzt den direkt injizierten Service aus dem Primary Constructor
        var result = await chatCompletion.GetChatMessageContentAsync(chatHistory);
        return result.Content ?? string.Empty;
    }
}