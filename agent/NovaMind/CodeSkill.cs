using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

/// <summary>
/// Skill zur Analyse und Verarbeitung von C#-Code-Dateien mittels Semantic Kernel.
/// </summary>
public class CodeSkill(IChatCompletionService chat) // 🚀 C# 12: Primary Constructor spart Boilerplate-DI-Code!
{
    [KernelFunction]
    public string ReadCode(string path) => SafeReadFile(path);

    [KernelFunction]
    public async Task<string> ExplainCode(string path, string lang)
    {
        var code = SafeReadFile(path);
        if (code.StartsWith("Fehler") || code.StartsWith("File not found")) 
            return code;

        var systemPrompt = GetPrompt(lang, 
            de: "Du bist ein professioneller Softwareentwickler. Erkläre den folgenden Code klar und verständlich.",
            en: "You are a professional software engineer. Explain the following code clearly and simply."
        );

        return await CallLlmAsync(systemPrompt, $"Erkläre diesen Code:\n\n{code}");
    }

    [KernelFunction]
    public async Task<string> FindIssues(string path, string lang)
    {
        var code = SafeReadFile(path);
        if (code.StartsWith("Fehler") || code.StartsWith("File not found")) 
            return code;

        var systemPrompt = GetPrompt(lang,
            de: "Du bist ein erfahrener Code-Reviewer. Finde Probleme, Risiken und Anti-Patterns im folgenden Code.",
            en: "You are an experienced code reviewer. Identify issues, risks, and anti-patterns in the following code."
        );

        return await CallLlmAsync(systemPrompt, $"Analysiere diesen Code:\n\n{code}");
    }

    [KernelFunction]
    public async Task<string> RefactorCode(string path, string lang)
    {
        var code = SafeReadFile(path);
        if (code.StartsWith("Fehler") || code.StartsWith("File not found")) 
            return code;

        var systemPrompt = GetPrompt(lang,
            de: "Du bist ein Senior-Softwareentwickler. Schreibe eine verbesserte Version des folgenden Codes.",
            en: "You are a senior software engineer. Write an improved version of the following code."
        );

        return await CallLlmAsync(systemPrompt, $"Refactore diesen Code:\n\n{code}");
    }

    [KernelFunction]
    public async Task<string> ToCobol(string path)
    {
        var code = SafeReadFile(path);
        if (code.StartsWith("Fehler") || code.StartsWith("File not found")) 
            return code;

        var systemPrompt = "Du bist ein Experte für Software-Migration. Übersetze den folgenden Programmcode VOLLSTÄNDIG und ZEILE FÜR ZEILE in sauberen, standardkonformen COBOL-Code. Kürze den Code NIEMALS mit '...' ab. Definiere alle Variablen in der DATA DIVISION und bilde die gesamte Programmlogik lückenlos ab. Gib NUR den reinen COBOL-Code zurück.";

        return await CallLlmAsync(systemPrompt, $"Übersetze diesen Code nach COBOL:\n\n{code}");
    }


    #region Private Helpers

    /// <summary>
    /// Liest eine Datei sicher ein und fängt Dateisystem-Exceptions ab (Vermeidung von TOCTOU-Bugs).
    /// </summary>
    private static string SafeReadFile(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (FileNotFoundException)
        {
            return $"File not found: {path}";
        }
        catch (Exception ex)
        {
            return $"Fehler beim Lesen der Datei '{path}': {ex.Message}";
        }
    }

    /// <summary>
    /// Führt den eigentlichen LLM-Chat-Call DRY-konform aus.
    /// </summary>
    private async Task<string> CallLlmAsync(string systemPrompt, string userMessage)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(userMessage);

        var result = await chat.GetChatMessageContentAsync(history);
        return result.Content ?? string.Empty;
    }

    /// <summary>
    /// Bestimmt dynamisch den Prompt basierend auf der Zielsprache.
    /// </summary>
    private static string GetPrompt(string lang, string de, string en) =>
        lang.Equals("German", StringComparison.OrdinalIgnoreCase) ? de : en;

    #endregion
}