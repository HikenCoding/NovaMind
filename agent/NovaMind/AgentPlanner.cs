using System;
using System.Collections.Generic;

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
Erstelle einen Plan in JSON.
Nutze nur Skills, die existieren:
- CodeSkill (ExplainCode, FindIssues, RefactorCode)
- PdfSkill (ReadPdf, SearchPdf, SummarizePdf)
- MemorySkill (Remember, Forget, Search)
- FileSkill (ReadFile, WriteFile, ListFiles)

Antwortformat NUR JSON:
{
  ""steps"": [
    {
      ""description"": ""..."",
      ""skill"": ""..."",
      ""function"": ""..."",
      ""arguments"": { ... }
    }
  ]
}";

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

                foreach (var arg in step.GetProperty("arguments").EnumerateObject())
                {
                    agentStep.Arguments[arg.Name] = arg.Value.GetString() ?? "";
                }

                plan.Steps.Add(agentStep);
            }
        }
        catch
        {
            // Fallback: kein Plan erzeugt
        }

        return plan;
    }


    }
