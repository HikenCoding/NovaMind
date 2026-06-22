using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;

public class AgentStep
{
    public string Description { get; set; } = "";
    public string SkillName { get; set; } = "";
    public string FunctionName { get; set; } = "";
    public Dictionary<string, string> Arguments { get; set; } = new();
}

public class AgentPlan
{
    public string OriginalRequest { get; set; } = "";
    public List<AgentStep> Steps { get; set; } = new();
}

public static class AgentPlanner
{
    // 1) Offizielle Funktionen aller Skills
    private static readonly Dictionary<string, (string skill, string function)> KnownFunctions = new()
    {
        // FileSkill
        ["readfile"] = ("FileSkill", "ReadFile"),
        ["writefile"] = ("FileSkill", "WriteFile"),
        ["listfiles"] = ("FileSkill", "ListFiles"),
        ["deletefile"] = ("FileSkill", "DeleteFile"),

        // PdfSkill
        ["readpdf"] = ("PdfSkill", "ReadPdf"),
        ["searchpdf"] = ("PdfSkill", "SearchPdf"),
        ["summarizepdf"] = ("PdfSkill", "SummarizePdf"),

        // CodeSkill
        ["readcode"] = ("CodeSkill", "ReadCode"),
        ["explaincode"] = ("CodeSkill", "ExplainCode"),
        ["findissues"] = ("CodeSkill", "FindIssues"),
        ["refactorcode"] = ("CodeSkill", "RefactorCode"),

        // MemorySkill
        ["remember"] = ("MemorySkill", "Remember"),
        ["forget"] = ("MemorySkill", "Forget"),
        ["search"] = ("MemorySkill", "Search"),

        // ReflectSkill
        ["reflect"] = ("ReflectSkill", "Reflect")
    };

    // 2) Aliase für erfundene Funktionsnamen
    private static readonly Dictionary<string, (string skill, string function)> FunctionAliases = new()
    {
        // CodeSkill Aliases
        ["extracttodos"] = ("CodeSkill", "FindIssues"),
        ["extractcomments"] = ("CodeSkill", "FindIssues"),
        ["gettodos"] = ("CodeSkill", "FindIssues"),
        ["todoanalysis"] = ("CodeSkill", "FindIssues"),

        // FileSkill Aliases
        ["loadfile"] = ("FileSkill", "ReadFile"),
        ["openfile"] = ("FileSkill", "ReadFile"),
        ["load"] = ("FileSkill", "ReadFile"),

        // PdfSkill Aliases
        ["loadpdf"] = ("PdfSkill", "ReadPdf"),
        ["openpdf"] = ("PdfSkill", "ReadPdf")
    };


    // 3) Valid Skills (für Validator)
    private static readonly Dictionary<string, List<string>> ValidSkills = new()
    {
        ["CodeSkill"] = new() { "ExplainCode", "FindIssues", "RefactorCode", "ReadCode" },
        ["PdfSkill"] = new() { "ReadPdf", "SearchPdf", "SummarizePdf" },
        ["MemorySkill"] = new() { "Remember", "Forget", "Search" },
        ["FileSkill"] = new() { "ReadFile", "WriteFile", "ListFiles", "DeleteFile" },
        ["ReflectSkill"] = new() { "Reflect" }
    };

    // 4) Simple Planner (Fallback)
    public static AgentPlan CreateSimplePlan(string input, string lang)
    {
        var plan = new AgentPlan { OriginalRequest = input };

        if (input.Contains("analysiere", StringComparison.OrdinalIgnoreCase))
        {
            var parts = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            string path = parts[^1];

            plan.Steps.Add(new AgentStep
            {
                Description = $"Erkläre den Code in {path}",
                SkillName = "CodeSkill",
                FunctionName = "ExplainCode",
                Arguments = new() { ["path"] = path, ["lang"] = lang }
            });

            plan.Steps.Add(new AgentStep
            {
                Description = $"Finde Probleme im Code {path}",
                SkillName = "CodeSkill",
                FunctionName = "FindIssues",
                Arguments = new() { ["path"] = path, ["lang"] = lang }
            });

            plan.Steps.Add(new AgentStep
            {
                Description = "Reflektiere das Gesamtergebnis",
                SkillName = "ReflectSkill",
                FunctionName = "Reflect",
                Arguments = new()
            });

            return plan;
        }

        return plan;
    }

    // 5) LLM Planner
    public static async Task<AgentPlan> CreateLLMPlanAsync(
        string input,
        string lang,
        IChatCompletionService chat)
    {
        string forcedSkill = DetectSkill(input);
        var systemPrompt = BuildSystemPrompt(forcedSkill);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage($"Erstelle einen Plan für diese Anfrage:\n{input}");

        var response = await chat.GetChatMessageContentAsync(chatHistory);
        var json = SanitizeJson(response.Content ?? "{}");

        AgentPlan plan = new AgentPlan { OriginalRequest = input };

        try
        {
            using var doc = JsonDocument.Parse(json);
            var steps = doc.RootElement.GetProperty("steps");

            foreach (var step in steps.EnumerateArray())
            {
                var agentStep = new AgentStep
                {
                    Description = step.GetProperty("description").GetString() ?? "",
                    SkillName = step.GetProperty("skill").GetString() ?? "",
                    FunctionName = step.GetProperty("function").GetString() ?? "",
                    Arguments = new()
                };


                // UNIVERSAL AUTO-FIX für erfundene Funktionsnamen
                string fnLower = agentStep.FunctionName.ToLower();

                if (FunctionAliases.TryGetValue(fnLower, out var alias))
                {
                    Console.WriteLine($"→ Auto-Fix: '{agentStep.FunctionName}' existiert nicht. Verwende {alias.skill}.{alias.function}.");
                    agentStep.SkillName = alias.skill;
                    agentStep.FunctionName = alias.function;
                }
                else if (!KnownFunctions.ContainsKey(fnLower))
                {
                    Console.WriteLine($"→ Auto-Fix: '{agentStep.FunctionName}' ist unbekannt. Verwende CodeSkill.FindIssues als Fallback.");
                    agentStep.SkillName = "CodeSkill";
                    agentStep.FunctionName = "FindIssues";
                }

                // Argumente einlesen
                if (step.TryGetProperty("arguments", out var argsElement))
                {
                    foreach (var arg in argsElement.EnumerateObject())
                        agentStep.Arguments[arg.Name] = arg.Value.GetString() ?? "";
                }

                // Auto-Fix: path ergänzen
                if (!agentStep.Arguments.ContainsKey("path"))
                {
                    var file = ExtractFileNameFromInput(input);
                    if (file != null)
                        agentStep.Arguments["path"] = file;
                }

                // Auto-Fix: lang ergänzen
                if (!agentStep.Arguments.ContainsKey("lang"))
                    agentStep.Arguments["lang"] = lang;

                plan.Steps.Add(agentStep);
            }
        }
        catch
        {
            Console.WriteLine("⚠️ Der KI‑Planner konnte keinen gültigen JSON‑Plan erzeugen.");
            Console.WriteLine(json);
        }

        // Reflect-Step anhängen
        plan.Steps.Add(new AgentStep
        {
            Description = "Reflektiere das Gesamtergebnis",
            SkillName = "ReflectSkill",
            FunctionName = "Reflect",
            Arguments = new()
        });

        return plan;
    }

    // Hilfsfunktionen
    private static string? ExtractFileNameFromInput(string input)
    {
        foreach (var p in input.Split(" ", StringSplitOptions.RemoveEmptyEntries))
            if (p.EndsWith(".pdf") || p.EndsWith(".cs"))
                return p;

        return null;
    }

    private static string SanitizeJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "{}";

        int start = raw.IndexOf('{');
        int end = raw.LastIndexOf('}');

        if (start < 0 || end < 0 || end <= start)
            return "{}";

        string json = raw.Substring(start, end - start + 1).Trim();

        int openBraces = 0, openBrackets = 0;

        foreach (char c in json)
        {
            if (c == '{') openBraces++;
            if (c == '}') openBraces--;
            if (c == '[') openBrackets++;
            if (c == ']') openBrackets--;
        }

        while (openBrackets-- > 0) json += "]";
        while (openBraces-- > 0) json += "}";

        return json;
    }

    private static string DetectSkill(string input)
    {
        input = input.ToLower();

        if (input.Contains(".pdf") || input.Contains("pdf"))
            return "PdfSkill";

        if (input.Contains(".cs") || input.Contains("code") || input.Contains("todo"))
            return "CodeSkill";

        if (input.Contains("datei") || input.Contains("ordner"))
            return "FileSkill";

        if (input.Contains("memory") || input.Contains("merken"))
            return "MemorySkill";

        return "Auto";
    }

    private static string BuildSystemPrompt(string forcedSkill)
    {
        string basePrompt = @"
Du bist ein KI-Agent Planner.
Du erzeugst immer JSON im Format:
{
  ""steps"": [
    {
      ""description"": ""..."",
      ""skill"": ""..."",
      ""function"": ""..."",
      ""arguments"": { ... }
    }
  ]
}
";

        if (forcedSkill != "Auto")
        {
            basePrompt += $@"
WICHTIG:
Der Benutzer möchte eindeutig eine Aufgabe, die zu {forcedSkill} gehört.
Erzeuge NUR Schritte mit diesem Skill, außer der letzte Schritt (ReflectSkill).
";
        }

        return basePrompt;
    }
}
