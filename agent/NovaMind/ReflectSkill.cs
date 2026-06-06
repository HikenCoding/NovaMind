using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

public class ReflectSkill
{
    private readonly IChatCompletionService _chat;

    public ReflectSkill(IChatCompletionService chat)
    {
        _chat = chat;
    }

    [KernelFunction]
    public async Task<string> Reflect(string input)
    {
        var chat = new ChatHistory();
        chat.AddSystemMessage("Du bist ein Reflexionsmodul. Analysiere das Ergebnis und gib eine kurze Bewertung ab.");
        chat.AddUserMessage($"Reflektiere dieses Ergebnis:\n\n{input}");

        var result = await _chat.GetChatMessageContentAsync(chat);
        return result.Content ?? "";
    }
}
