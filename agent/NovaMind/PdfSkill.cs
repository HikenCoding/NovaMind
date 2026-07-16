using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using UglyToad.PdfPig;

public class PdfSkill
{
    // Zentrale Hilfsmethode zur Fehlerbehandlung (Verhindert doppelten Code in den SK-Funktionen)
    private string GetPdfTextOrError(string path, out string? text)
    {
        text = null;
        try
        {
            // TOCTOU-Schutz: Wir versuchen direkt zu lesen, fehlende Dateien fangen wir im Catch ab
            text = ExtractText(path);
            if (string.IsNullOrWhiteSpace(text))
            {
                return "⚠️ Die PDF-Datei enthält keinen lesbaren Text.";
            }
            return string.Empty; // Kein Fehler gefunden
        }
        catch (FileNotFoundException)
        {
            return $"❌ Datei nicht gefunden: {path}";
        }
        catch (Exception ex)
        {
            return $"❌ Fehler beim Lesen der PDF: {ex.Message}";
        }
    }

    [KernelFunction]
    public string ReadPdf(string path)
    {
        Console.WriteLine($"[PdfSkill] ReadPdf wurde mit path = '{path}' aufgerufen.");
        
        var error = GetPdfTextOrError(path, out var text);
        return string.IsNullOrEmpty(error) ? text! : error;
    }

    [KernelFunction]
    public string SearchPdf(string path, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return "⚠️ Kein Suchbegriff angegeben.";

        var error = GetPdfTextOrError(path, out var text);
        if (!string.IsNullOrEmpty(error))
            return error;

        // Zeilenweise Suche optimiert durchführen
        var lines = text!.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var sb = new StringBuilder();
        var matchCount = 0;

        foreach (var line in lines)
        {
            if (line.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine(line.Trim());
                matchCount++;
            }
        }

        if (matchCount == 0)
            return $"🔍 Keine Treffer für den Begriff '{search}' gefunden.";

        return $"📄 Gefundene Übereinstimmungen ({matchCount}):\n{sb}";
    }

    [KernelFunction]
    public async Task<string> SummarizePdf(string path, Kernel kernel)
    {
        var error = GetPdfTextOrError(path, out var text);
        if (!string.IsNullOrEmpty(error))
            return error;

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();

        // Sprache erkennen (Nutzt den vorhandenen LanguageDetector deines Projekts)
        var lang = LanguageDetector.Detect(text!);

        // System-Prompt je nach Sprache dynamisch anpassen
        var systemPrompt = lang == "German"
            ? "Du bist NovaMind. Dir wird der bereits extrahierte Text aus einer PDF-Datei übergeben. Fasse diesen Text klar, präzise und strukturiert auf Deutsch zusammen. Behaupte nicht, dass du keinen Zugriff auf die Datei hast, denn der Text liegt dir unten vollständig vor!"
            : "You are NovaMind. You are provided with the pre-extracted text from a PDF file. Summarize this text clearly and concisely in English. Do not claim you cannot access the file, as the text is provided directly below!";

        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage($"Hier ist der extrahierte PDF-Inhalt zur Zusammenfassung:\n\n{text}");

        var response = await chat.GetChatMessageContentAsync(history);
        return response.Content ?? "⚠️ Es konnte keine Zusammenfassung generiert werden.";
    }

    // Interne Hilfsfunktion: Liest die PDF Seite für Seite aus
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