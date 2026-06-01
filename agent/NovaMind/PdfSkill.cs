using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

public class PdfSkill
{
    [KernelFunction]
    public string ReadPdf(string path)
    {
        if (!File.Exists(path))
            return $"File not found: {path}";

        return ExtractText(path);
    }

    [KernelFunction]
    public string SearchPdf(string path, string search)
    {
        if (!File.Exists(path))
            return $"File not found: {path}";

        var text = ExtractText(path);

        var matches = text
            .Split('\n')
            .Where(line => line.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return $"No matches found for '{search}'.";

        return "Matches:\n" + string.Join("\n", matches);
    }

    [KernelFunction]
    public async Task<string> SummarizePdf(string path, Kernel kernel)
    {
        if (!File.Exists(path))
            return $"File not found: {path}";

        var text = ExtractText(path);

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

        return response.Content ?? "No summary generated.";
    }

    private string ExtractText(string path)
    {
        var result = new System.Text.StringBuilder();

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
