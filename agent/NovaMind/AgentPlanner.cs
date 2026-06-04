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
    public static AgentPlan CreateSimplePlan(string input)
    {
        var plan = new AgentPlan { OriginalRequest = input };

        // Code erklären
        if (input.Contains("code erklären", StringComparison.OrdinalIgnoreCase) ||
            input.Contains("code explain", StringComparison.OrdinalIgnoreCase))
        {
            var parts = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            string path = parts[^1]; // letztes Wort = Dateiname

            plan.Steps.Add(new AgentStep
            {
                Description = $"Erkläre den Code in {path}",
                SkillName = "CodeSkill",
                FunctionName = "ExplainCode",
                Arguments = new()
                {
                    ["path"] = path,
                    ["lang"] = "de" // später dynamisch
                }
            });

            return plan;
        }

        return plan;
    }
}
