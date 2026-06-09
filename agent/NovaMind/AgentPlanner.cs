using System;
using System.Collections.Generic;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

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
        private static readonly Dictionary<string, List<string>> ValidSkills = new()
    {
        ["CodeSkill"] = new() { "ExplainCode", "FindIssues", "RefactorCode" },
        ["PdfSkill"] = new() { "ReadPdf", "SearchPdf", "SummarizePdf" },
        ["MemorySkill"] = new() { "Remember", "Forget", "Search" },
        ["FileSkill"] = new() { "ReadFile", "WriteFile", "ListFiles" }
    };

    public static AgentPlan CreateSimplePlan(string input, string lang)
    {
        var plan = new AgentPlan { OriginalRequest = input };

       // Code analysieren (Erklärung + Issues)
    if (input.Contains("analysiere", StringComparison.OrdinalIgnoreCase) ||
        input.Contains("analyse", StringComparison.OrdinalIgnoreCase))
    {
        var parts = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        string path = parts[^1];

        // Schritt 1: Code erklären
        plan.Steps.Add(new AgentStep
        {
            Description = $"Erkläre den Code in {path}",
            SkillName = "CodeSkill",
            FunctionName = "ExplainCode",
            Arguments = new()
            {
                ["path"] = path,
                ["lang"] = "de"
            }
        });

        // Schritt 2: Probleme finden
        plan.Steps.Add(new AgentStep
        {
            Description = $"Finde Probleme im Code {path}",
            SkillName = "CodeSkill",
            FunctionName = "FindIssues",
            Arguments = new()
            {
                ["path"] = path,
                ["lang"] = "de"
            }
        });


        plan.Steps.Add(new AgentStep
        {
        Description = "Reflektiere das Gesamtergebnis",
        SkillName = "ReflectSkill",
        FunctionName = "Reflect",
            Arguments = new()
            {
            ["input"] = "" // wird später gefüllt
            }
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
var systemPrompt = @"
Du bist ein KI-Agenten-Planer. Deine Aufgabe ist es, Benutzeranfragen in eine logische Sequenz von Schritten (einen Plan) zu übersetzen.

HARTE REGELN:
- Antworte mit REINEM JSON.
- KEIN Text vor oder nach dem JSON.
- KEIN Markdown (KEINE ```json oder ``` Backticks).
- KEINE Erklärungen, KEINE Kommentare, KEINE Einleitung.

Das JSON MUSS GENAU SO aufgebaut sein:
{
  ""steps"": [
    {
      ""description"": ""Kurze Beschreibung des Schritts auf Deutsch"",
      ""skill"": ""SkillName"",
      ""function"": ""FunctionName"",
      ""arguments"": {
        ""key"": ""value""
      }
    }
  ]
}

WICHTIGE VERHALTENSREGELN:
1. Wenn eine Anfrage MEHRERE Aktionen erfordert, erstelle MEHRERE Schritte im Array. (Beispiel: ""Lese Datei.cs und merke dir den Inhalt"" -> Schritt 1: Datei lesen, Schritt 2: Inhalt im Memory speichern).
2. Nutze den CodeSkill NUR, wenn explizit nach Analyse, Refactoring oder Erklärung von Code gefragt wird. Wenn der User nur Daten aus einer .cs-Datei lesen will, nutze FileSkill oder CodeSkill.ReadCode.
3. Wenn du keine Argumente für eine Funktion hast, setze: ""arguments"": {}

VERFÜGBARE SKILLS UND DEREN PARAMETER:

PdfSkill:
- ReadPdf -> Liest Text aus einer PDF. (Erfordert Argument: ""path"")
- SearchPdf -> Sucht nach einem Begriff in einer PDF. (Erfordert zwingend BEIDE Argumente: ""path"" UND ""search"")
- SummarizePdf -> Erstellt eine Zusammenfassung einer PDF. (Erfordert Argument: ""path"")

CodeSkill:
- ReadCode -> Liest den Quellcode einer Datei ein. (Erfordert Argument: ""path"")
- ExplainCode -> Erklärt Quellcode. (NUR nutzen bei expliziter Frage nach Erklärung! Erfordert Argument: ""path"")
- FindIssues -> Sucht nach Bugs oder Anti-Patterns im Code. (NUR nutzen bei expliziter Frage nach Fehlersuche! Erfordert Argument: ""path"")
- RefactorCode -> Schlägt Code-Verbesserungen vor. (NUR nutzen bei expliziter Frage nach Refactoring! Erfordert Argument: ""path"")

MemorySkill:
- Remember -> Speichert Informationen dauerhaft im Gedächtnis des Agenten. (WICHTIG: Erfordert zwingend das Argument ""input"" mit dem zu speichernden Text!)
- Forget -> Löscht Informationen aus dem Gedächtnis. (Erfordert Argument: ""input"")
- Search -> Sucht in gespeicherten Erinnerungen. (Erfordert Argument: ""text"")

FileSkill:
- ReadFile -> Liest eine normale Textdatei. (Erfordert Argument: ""path"")
- WriteFile -> Schreibt eine Datei. (Erfordert Argumente: ""path"" und ""content"")
- ListFiles -> Listet Dateien in einem Ordner auf. (Erfordert Argument: ""path"")

BEISPIEL-PLÄNE:

Anfrage: ""speichere die TODOs aus Program.cs im Memory""
{
  ""steps"": [
    {
      ""description"": ""Lese den Quellcode aus Program.cs"",
      ""skill"": ""CodeSkill"",
      ""function"": ""ReadCode"",
      ""arguments"": {
        ""path"": ""Program.cs""
      }
    },
    {
      ""description"": ""Extrahiere TODOs und speichere sie im Gedächtnis"",
      ""skill"": ""MemorySkill"",
      ""function"": ""Remember"",
      ""arguments"": {
        ""input"": ""TODOs aus Program.cs extrahieren""
      }
    }
  ]
}

Anfrage: ""Suche nach Fehlern in der datei.pdf""
{
  ""steps"": [
    {
      ""description"": ""Suche nach Fehlern in der PDF-Datei"",
      ""skill"": ""PdfSkill"",
      ""function"": ""SearchPdf"",
      ""arguments"": {
        ""path"": ""datei.pdf"",
        ""search"": ""Fehler""
      }
    }
  ]
}

Gib NUR das fertige JSON-Objekt zurück. ";

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage($"Erstelle einen Plan für diese Anfrage:\n{input}");

        var response = await chat.GetChatMessageContentAsync(chatHistory);
        var json = response.Content ?? "{}";
        json = SanitizeJson(json);

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
                    Arguments = new Dictionary<string, string>()
                };

                // Wenn arguments fehlen → leeres Dictionary
                if (!step.TryGetProperty("arguments", out var argsElement))
                {
                    agentStep.Arguments = new Dictionary<string, string>();
                }

                // Auto-Fix: Wenn path fehlt, aber der User eine Datei erwähnt
                if (!agentStep.Arguments.ContainsKey("path"))
                {
                    var file = ExtractFileNameFromInput(input);
                    if (file != null)
                    {
                        agentStep.Arguments["path"] = file;
                    }
                }

                // Auto-Fix für PdfSkill.SearchPdf: Wenn 'search' fehlt, aber die Funktion es verlangt
                if (agentStep.FunctionName == "SearchPdf" && !agentStep.Arguments.ContainsKey("search"))
                {
                    // Falls das Wort "todo" im Input vorkommt, nimm das, sonst ein Standardwort oder den gesamten Input ohne den Dateinamen
                    if (input.Contains("todo", StringComparison.OrdinalIgnoreCase))
                    {
                        agentStep.Arguments["search"] = "TODO";
                    }
                    else
                    {
                        agentStep.Arguments["search"] = "Anfrage"; // Fallback, damit der Kernel nicht abstürzt
                    }
                    Console.WriteLine($"→ Auto-Fix: Fehlenden Suchbegriff ergänzt: '{agentStep.Arguments["search"]}'");
                }

                // arguments optional
            if (step.TryGetProperty("arguments", out var argsElement2))
            {
                foreach (var arg in argsElement2.EnumerateObject())
                {
                    agentStep.Arguments[arg.Name] = arg.Value.GetString() ?? "";
                }
            }
            else
            {
                agentStep.Arguments = new Dictionary<string, string>();
            }

            // Skill/Funktion validieren
            if (!ValidSkills.ContainsKey(agentStep.SkillName) ||
                !ValidSkills[agentStep.SkillName].Contains(agentStep.FunctionName))
            {
                Console.WriteLine($"⚠️ Ungültiger Step erkannt: {agentStep.SkillName}.{agentStep.FunctionName}");

                // Auto-Fix: richtige Funktion suchen
                foreach (var skill in ValidSkills)
                {
                    if (skill.Value.Contains(agentStep.FunctionName))
                    {
                        Console.WriteLine($"→ Auto-Fix: Setze Skill auf {skill.Key}");
                        agentStep.SkillName = skill.Key;
                        break;
                    }
                }
            }

            // Auto-Fix: Wenn lang fehlt → automatisch setzen
            if (!agentStep.Arguments.ContainsKey("lang"))
            {
                agentStep.Arguments["lang"] = lang;
            }


                plan.Steps.Add(agentStep);
            }
        }
       catch (Exception ex)
    {
    Console.WriteLine("⚠️ Der KI‑Planner konnte keinen gültigen JSON‑Plan erzeugen.");
    Console.WriteLine("Antwort des Modells:");
    Console.WriteLine(json);
    Console.WriteLine("Fehler:");
    Console.WriteLine(ex.Message);
    }


    // Reflect-Step immer anhängen
plan.Steps.Add(new AgentStep
{
    Description = "Reflektiere das Gesamtergebnis",
    SkillName = "ReflectSkill",
    FunctionName = "Reflect",
    Arguments = new Dictionary<string, string>()
});

        return plan;
    }


private static string? ExtractFileNameFromInput(string input)
{
    var parts = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
    foreach (var p in parts)
    {
        if (p.EndsWith(".pdf") || p.EndsWith(".cs"))
            return p;
    }
    return null;
}

private static string SanitizeJson(string raw)
{
    if (string.IsNullOrWhiteSpace(raw))
        return "{}";

    // Finde erstes '{'
    int start = raw.IndexOf('{');
    if (start < 0)
        return "{}";

    // Finde letztes '}'
    int end = raw.LastIndexOf('}');
    if (end < 0 || end <= start)
        return "{}";

    // Schneide den JSON-Teil heraus
    string json = raw.Substring(start, end - start + 1).Trim();

    // --- AUTO-REPARATUR FÜR UNVOLLSTÄNDIGES JSON ---
    // Zähle, ob alle geöffneten Klammern auch geschlossen werden
    int openBraces = 0;
    int openBrackets = 0;

    foreach (char c in json)
    {
        if (c == '{') openBraces++;
        if (c == '}') openBraces--;
        if (c == '[') openBrackets++;
        if (c == ']') openBrackets--;
    }

    // Wenn eckige Klammern offen sind (z.B. bei "steps": [ )
    while (openBrackets > 0)
    {
        json += "]";
        openBrackets--;
    }

    // Wenn geschweifte Klammern offen sind
    while (openBraces > 0)
    {
        json += "}";
        openBraces--;
    }

    return json;
}



    }
