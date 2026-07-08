using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text;

public class PdfSkill
{
    // 1) PDF lesen
    [KernelFunction]
    public string ReadPdf(string path)
    {
        if (!File.Exists(path))
            return $"❌ File not found: {path}";

        try
        {
            return ExtractText(path);
        }
        catch (Exception ex)
        {
            return $"❌ Error reading PDF: {ex.Message}";
        }
    }

    // 2) PDF durchsuchen
    [KernelFunction]
    public string SearchPdf(string path, string search)
    {
        if (!File.Exists(path))
            return $"❌ File not found: {path}";

        if (string.IsNullOrWhiteSpace(search))
            return "⚠️ No search term provided.";

        string text;

        try
        {
            text = ExtractText(path);
        }
        catch (Exception ex)
        {
            return $"❌ Error reading PDF: {ex.Message}";
        }

        var matches = text
            .Split('\n')
            .Where(line => line.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return $"🔍 No matches found for '{search}'.";

        return "📄 Matches:\n" + string.Join("\n", matches);
    }

    // 3) PDF zusammenfassen (LLM)
    [KernelFunction]
    public async Task<string> SummarizePdf(string path, Kernel kernel)
    {
        if (!File.Exists(path))
            return $"❌ File not found: {path}";

        string text;

        try
        {
            text = ExtractText(path);
        }
        catch (Exception ex)
        {
            return $"❌ Error reading PDF: {ex.Message}";
        }

        if (string.IsNullOrWhiteSpace(text))
            return "⚠️ PDF contains no readable text.";

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();

        var lang = LanguageDetector.Detect(text);

        history.AddSystemMessage(
            lang == "German"
            ? "Du bist NovaMind. Fasse den folgenden PDF‑Inhalt klar und präzise auf Deutsch zusammen."
            : "You are NovaMind. Summarize the following PDF content clearly and concisely in English."
        );

        history.AddUserMessage(text);

        var response = await chat.GetChatMessageContentAsync(history);

        return response.Content ?? "⚠️ No summary generated.";
    }

    // Hilfsfunktion: PDF‑Text extrahieren
    private string ExtractText(string path)
    {
        var result = new StringBuilder();

        using (var document = PdfDocument.Open(path))
        {
            foreach (var page in document.GetPages())
            {
                result.AppendLine(page.Text);
            }
        }

        return result.ToString();
    }
}
