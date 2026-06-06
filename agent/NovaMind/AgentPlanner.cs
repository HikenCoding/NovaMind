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

        return plan;
    }
            return plan;
        }
    }
