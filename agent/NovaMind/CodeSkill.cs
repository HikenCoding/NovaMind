using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

public class CodeSkill
{
    private readonly IChatCompletionService _chat;

    public CodeSkill(IChatCompletionService chat)
    {
        _chat = chat;
    }

    private string ReadFile(string path)
    {
        if (!File.Exists(path))
            return $"File not found: {path}";

        return File.ReadAllText(path);
    }

    [KernelFunction]
    public async Task<string> ReadCode(string path)
    {
        return ReadFile(path);
    }

    [KernelFunction]
    public async Task<string> ExplainCode(string path, string lang)
    {
        var code = ReadFile(path);
        if (code.StartsWith("File not found"))
            return code;

        var systemPrompt = lang == "de"
            ? "Du bist ein professioneller Softwareentwickler. Erkläre den folgenden Code klar und verständlich."
            : "You are a professional software engineer. Explain the following code clearly and simply.";

        var chat = new ChatHistory();
        chat.AddSystemMessage(systemPrompt);
        chat.AddUserMessage($"Erkläre diesen Code:\n\n{code}");

        var result = await _chat.GetChatMessageContentAsync(chat);
        return result.Content ?? "";
    }

    [KernelFunction]
    public async Task<string> FindIssues(string path, string lang)
    {
        var code = ReadFile(path);
        if (code.StartsWith("File not found"))
            return code;

        var systemPrompt = lang == "de"
            ? "Du bist ein erfahrener Code-Reviewer. Finde Probleme, Risiken und Anti-Patterns im folgenden Code."
            : "You are an experienced code reviewer. Identify issues, risks, and anti-patterns in the following code.";

        var chat = new ChatHistory();
        chat.AddSystemMessage(systemPrompt);
        chat.AddUserMessage($"Analysiere diesen Code:\n\n{code}");

        var result = await _chat.GetChatMessageContentAsync(chat);
        return result.Content ?? "";
    }

    [KernelFunction]
    public async Task<string> RefactorCode(string path, string lang)
    {
        var code = ReadFile(path);
        if (code.StartsWith("File not found"))
            return code;

        var systemPrompt = lang == "de"
            ? "Du bist ein Senior-Softwareentwickler. Schreibe eine verbesserte Version des folgenden Codes."
            : "You are a senior software engineer. Write an improved version of the following code.";

        var chat = new ChatHistory();
        chat.AddSystemMessage(systemPrompt);
        chat.AddUserMessage($"Refactore diesen Code:\n\n{code}");

        var result = await _chat.GetChatMessageContentAsync(chat);
        return result.Content ?? "";
    }
}
