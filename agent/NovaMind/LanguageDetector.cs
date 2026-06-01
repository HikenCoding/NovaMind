public static class LanguageDetector
{
    public static string Detect(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "German"; // Default

        // Wenn der Text Umlaute oder Sonderzeichen enthält → Deutsch
        if (input.Any(c => c > 127))
            return "German";

        // Wenn der Text nur ASCII enthält → Englisch
        return "English";
    }

    public static string GetSystemPrompt(string language)
    {
        return language == "German"
            ? "Du bist NovaMind, ein KI‑Agent. Antworte immer auf Deutsch."
            : "You are NovaMind, an AI agent. Always respond in English.";
    }
}
