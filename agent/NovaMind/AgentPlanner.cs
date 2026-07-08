using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
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
    private static readonly Dictionary<string, (string skill, string function)> KnownFunctions = new()
    {
        ["readfile"] = ("FileSkill", "ReadFile"),
        ["writefile"] = ("FileSkill", "WriteFile"),
        ["listfiles"] = ("FileSkill", "ListFiles"),
        ["deletefile"] = ("FileSkill", "DeleteFile"),

        ["readpdf"] = ("PdfSkill", "ReadPdf"),
        ["searchpdf"] = ("PdfSkill", "SearchPdf"),
        ["summarizepdf"] = ("PdfSkill", "SummarizePdf"),

        ["readcode"] = ("CodeSkill", "ReadCode"),
        ["explaincode"] = ("CodeSkill", "ExplainCode"),
        ["findissues"] = ("CodeSkill", "FindIssues"),
        ["refactorcode"] = ("CodeSkill", "RefactorCode"),

        ["remember"] = ("MemorySkill", "Remember"),
        ["forget"] = ("MemorySkill", "Forget"),
        ["search"] = ("MemorySkill", "Search"),

        ["reflect"] = ("ReflectSkill", "Reflect"),

        ["listdirectory"] = ("DirectorySkill", "ListDirectory"),
        ["analyzedirectory"] = ("DirectorySkill", "AnalyzeDirectory")
    };

    private static readonly Dictionary<string, (string skill, string function)> FunctionAliases = new()
    {
        ["extracttodos"] = ("CodeSkill", "FindIssues"),
        ["extractcomments"] = ("CodeSkill", "FindIssues"),
        ["gettodos"] = ("CodeSkill", "FindIssues"),

        ["loadfile"] = ("FileSkill", "ReadFile"),
        ["openfile"] = ("FileSkill", "ReadFile"),

        ["loadpdf"] = ("PdfSkill", "ReadPdf"),
        ["openpdf"] = ("PdfSkill", "ReadPdf"),
        ["öffnepdf"] = ("PdfSkill", "ReadPdf"),
        ["öffne_pdf"] = ("PdfSkill", "ReadPdf"),
        ["open pdf file"] = ("PdfSkill", "ReadPdf"),
        ["open"] = ("PdfSkill", "ReadPdf"),
        ["öffne"] = ("PdfSkill", "ReadPdf"),

        ["list_files"] = ("DirectorySkill", "ListDirectory"),
        ["listfiles"] = ("DirectorySkill", "ListDirectory"),
        ["listFiles"] = ("DirectorySkill", "ListDirectory"),
        ["gatherfiles"] = ("DirectorySkill", "ListDirectory"),
        ["scanfiles"] = ("DirectorySkill", "ListDirectory"),

        ["analyze_file"] = ("DirectorySkill", "AnalyzeDirectory"),
        ["analyze_files"] = ("DirectorySkill", "AnalyzeDirectory"),
        ["analyze_directory"] = ("DirectorySkill", "AnalyzeDirectory"),
        ["analyze_file_contents"] = ("DirectorySkill", "AnalyzeDirectory")
    };

    private static readonly Dictionary<string, List<string>> ValidSkills = new()
    {
        ["CodeSkill"] = new() { "ExplainCode", "FindIssues", "RefactorCode", "ReadCode" },
        ["PdfSkill"] = new() { "ReadPdf", "SearchPdf", "SummarizePdf" },
        ["MemorySkill"] = new() { "Remember", "Forget", "Search" },
        ["FileSkill"] = new() { "ReadFile", "WriteFile", "ListFiles", "DeleteFile" },
        ["ReflectSkill"] = new() { "Reflect" },
        ["DirectorySkill"] = new() { "ListDirectory", "AnalyzeDirectory" }
    };

    public static AgentPlan CreateSimplePlan(string input, string lang)
    {
        var plan = new AgentPlan { OriginalRequest = input };

        if (input.Contains("analysiere"))
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

            return plan;
        }

        return plan;
    }

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

                // PDF: Pfad IMMER aus der Eingabe ableiten, LLM-Argumente überschreiben
                if (forcedSkill == "PdfSkill")
                {
                    var file = ExtractFileNameFromInput(input); // liest .pdf aus "/agent öffne rechnung.pdf"
                    if (file != null)
                    {
                        Console.WriteLine($"[Planner] Erzwinge PDF-Pfad: {file}");
                        agentStep.Arguments["path"] = file;
                    }
                }


                // ReflectSkill darf NIE ohne input ausgeführt werden
                if (agentStep.SkillName == "ReflectSkill")
                {
                    agentStep.FunctionName = "Reflect";

                    // Falls der LLM keinen input gesetzt hat → setzen wir einen
                    if (!agentStep.Arguments.ContainsKey("input"))
                        agentStep.Arguments["input"] = agentStep.Description;
                }



                // PATCH: Beschreibung darf NIE Funktionsname sein
                if (agentStep.FunctionName.Equals(agentStep.Description, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"→ Auto-Fix: Beschreibung '{agentStep.Description}' wurde fälschlich als Funktionsname erkannt. Setze Funktion auf ReadPdf.");
                    agentStep.FunctionName = "ReadPdf";
                    agentStep.SkillName = "PdfSkill";
                }

                // PDF erzwingen
                if (forcedSkill == "PdfSkill" && agentStep.SkillName != "ReflectSkill")
                {
                    agentStep.SkillName = "PdfSkill";

                    if (string.IsNullOrWhiteSpace(agentStep.FunctionName))
                        agentStep.FunctionName = "ReadPdf";
                }

                if (string.IsNullOrWhiteSpace(agentStep.FunctionName))
                {
                    Console.WriteLine("→ Auto-Fix: Leerer Funktionsname → Step wird entfernt.");
                    continue;
                }

                string fnLower = agentStep.FunctionName.ToLower();

                if (FunctionAliases.TryGetValue(fnLower, out var alias))
                {
                    agentStep.SkillName = alias.skill;
                    agentStep.FunctionName = alias.function;
                }
                else if (!KnownFunctions.ContainsKey(fnLower))
                {
                    if (forcedSkill == "PdfSkill")
                    {
                        agentStep.SkillName = "PdfSkill";
                        agentStep.FunctionName = "ReadPdf";
                    }
                    else
                    {
                        agentStep.SkillName = "DirectorySkill";
                        agentStep.FunctionName = "ListDirectory";
                    }
                }

                if (step.TryGetProperty("arguments", out var argsElement))
                {
                    foreach (var arg in argsElement.EnumerateObject())
                        agentStep.Arguments[arg.Name] = arg.Value.GetString() ?? "";
                }

                // PDF path setzen
                if (agentStep.SkillName == "PdfSkill" &&
                    !agentStep.Arguments.ContainsKey("path"))
                {
                    var file = ExtractFileNameFromInput(input);
                    if (file != null)
                        agentStep.Arguments["path"] = file;
                }

                if (!agentStep.Arguments.ContainsKey("lang"))
                    agentStep.Arguments["lang"] = lang;

                plan.Steps.Add(agentStep);
            }
        }
        catch
        {
            return CreateSimplePlan(input, lang);
        }

        string reflectInput = "";
        foreach (var s in plan.Steps)
        {
            if (s.SkillName != "ReflectSkill")
                reflectInput += $"Step: {s.Description}\n";
        }

        plan.Steps.Add(new AgentStep
        {
            Description = "Reflektiere das Gesamtergebnis",
            SkillName = "ReflectSkill",
            FunctionName = "Reflect",
            Arguments = new() { ["input"] = reflectInput }
        });

        return plan;
    }

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

        return raw.Substring(start, end - start + 1).Trim();
    }

    private static string DetectSkill(string input)
    {
        input = input.ToLower();

        if (input.Contains(".pdf") || input.Contains("pdf"))
            return "PdfSkill";

        if (input.Contains(".cs") || input.Contains("code") || input.Contains("todo"))
            return "CodeSkill";

        if (input.Contains("ordner") || input.Contains("directory") || input.Contains("folder"))
            return "DirectorySkill";

        if (input.Contains("datei") || input.Contains("file"))
            return "FileSkill";

        if (input.Contains("memory") || input.Contains("merken"))
            return "MemorySkill";

        return "Auto";
    }

    private static string BuildSystemPrompt(string forcedSkill)
    {
        string basePrompt = @"
Du erzeugst IMMER gültiges JSON:
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
Kein Text außerhalb des JSON.
Kein '...' .
Jeder Step MUSS eine Funktion haben.
";

        if (forcedSkill != "Auto")
        {
            basePrompt += $@"
Erzeuge NUR Schritte mit {forcedSkill}, außer der letzte Schritt (ReflectSkill).
";
        }

        return basePrompt;
    }
}
