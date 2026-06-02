using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

public class CodeSkill
{
    private readonly IChatCompletionService _chat;
    private readonly LanguageDetector _lang;

    public CodeSkill(IChatCompletionService chat, LanguageDetector lang)
    {
        _chat = chat;
        _lang = lang;
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
    public async Task<string> ExplainCode(string path)
    {
        var code = ReadFile(path);
        if (code.StartsWith("File not found"))
            return code;

        var lang = _lang.DetectLanguage(code);
        var systemPrompt = lang == "de"
            ? "Du bist ein professioneller Softwareentwickler. Erkläre Code klar und verständlich."
            : "You are a professional software engineer. Explain code clearly and simply.";

        var chat = new ChatHistory();
        chat.AddSystemMessage(systemPrompt);
        chat.AddUserMessage($"Erkläre diesen Code:\n\n{code}");

        var result = await _chat.GetChatMessageContentAsync(chat);
        return result.ToString();
    }

    [KernelFunction]
    public async Task<string> FindIssues(string path)
    {
        var code = ReadFile(path);
        if (code.StartsWith("File not found"))
            return code;

        var lang = _lang.DetectLanguage(code);
        var systemPrompt = lang == "de"
            ? "Du bist ein erfahrener Code-Reviewer. Finde Probleme, Risiken und Anti-Patterns."
            : "You are an experienced code reviewer. Find issues, risks, and anti-patterns.";

        var chat = new ChatHistory();
        chat.AddSystemMessage(systemPrompt);
        chat.AddUserMessage($"Analysiere diesen Code auf Probleme:\n\n{code}");

        var result = await _chat.GetChatMessageContentAsync(chat);
        return result.ToString();
    }

    [KernelFunction]
    public async Task<string> RefactorCode(string path)
    {
        var code = ReadFile(path);
        if (code.StartsWith("File not found"))
            return code;

        var lang = _lang.DetectLanguage(code);
        var systemPrompt = lang == "de"
            ? "Du bist ein Senior-Softwareentwickler. Schreibe eine verbesserte Version des Codes."
            : "You are a senior software engineer. Write an improved version of the code.";

        var chat = new ChatHistory();
        chat.AddSystemMessage(systemPrompt);
        chat.AddUserMessage($"Refactore diesen Code:\n\n{code}");

        var result = await _chat.GetChatMessageContentAsync(chat);
        return result.ToString();
    }
}
