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
Du bist ein KI-Agenten-Planer.

HARTE REGELN:
- Antworte mit REINEM JSON.
- KEIN Text vor oder nach dem JSON.
- KEIN Markdown.
- KEINE Erklärungen.
- KEINE Kommentare.
- KEINE Backticks.
- KEINE Einleitung wie 'Here is the plan'.

Das JSON MUSS GENAU SO aussehen:

{
  ""steps"": [
    {
      ""description"": ""kurze Beschreibung"",
      ""skill"": ""SkillName"",
      ""function"": ""FunctionName"",
      ""arguments"": {
        ""path"": ""Dateiname.pdf""
      }
    }
  ]
}

ALLE Felder sind Pflicht:
- description (string)
- skill (string)
- function (string)
- arguments (object)

Wenn der User eine Datei erwähnt (*.pdf, *.cs):
- extrahiere den Dateinamen
- setze ihn in arguments.path

Wenn du keine Argumente kennst:
""arguments"": {}

Verfügbare Skills:

PdfSkill:
- ReadPdf
- SearchPdf
- SummarizePdf

CodeSkill:
- ExplainCode
- FindIssues
- RefactorCode

MemorySkill:
- Remember
- Forget
- Search

FileSkill:
- ReadFile
- WriteFile
- ListFiles

Gib NUR das JSON zurück.";

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage($"Erstelle einen Plan für diese Anfrage:\n{input}");

        var response = await chat.GetChatMessageContentAsync(chatHistory);
        var json = response.Content ?? "{}";

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


    }
