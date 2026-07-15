using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>
    /// Erstellt den Ausführungsplan über die LLM-Schnittstelle und wendet Auto-Fixes an.
    /// </summary>
    public static async Task<AgentPlan> CreateLLMPlanAsync(
        string input,
        string lang,
        IChatCompletionService chat)
    {
        var forcedSkill = DetectSkill(input);
        var systemPrompt = BuildSystemPrompt(forcedSkill);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage($"Erstelle einen Plan für diese Anfrage:\n{input}");

        var response = await chat.GetChatMessageContentAsync(chatHistory);
        var json = SanitizeJson(response.Content ?? "{}");

        var plan = new AgentPlan { OriginalRequest = input };

       try
        {
            var rawSteps = ParseJsonToSteps(json);
            foreach (var rawStep in rawSteps)
            {
                var processedStep = ProcessAndFixStep(rawStep, input, forcedSkill, lang);
                if (processedStep != null)
                {
                    plan.Steps.Add(processedStep);
                }
            }

            if (plan.Steps.Count == 0)
            {
                Console.WriteLine("⚠️ LLM lieferte leeres oder unbrauchbares JSON. Trigger Fallback...");
                plan = CreateSimplePlan(input, lang);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Fehler beim Parsen des JSON-Plans: {ex.Message}");
            plan = CreateSimplePlan(input, lang);
        }

        ApplyMemoryBooster(plan, input);

        return plan;
    }

    /// <summary>
    /// Statischer Fallback-Planer bei Syntaxfehlern oder Offline-Betrieb.
    /// </summary>
    public static AgentPlan CreateSimplePlan(string input, string lang)
    {
        var plan = new AgentPlan { OriginalRequest = input };
        var inputLower = input.ToLower();
        var file = ExtractFileNameFromInput(input);

        // Fallback 1: C#-Code-Analyse
        if (inputLower.Contains("analysiere") && file != null && file.EndsWith(".cs"))
        {
            plan.Steps.Add(new AgentStep { Description = $"Erkläre den Code in {file}", SkillName = "CodeSkill", FunctionName = "ExplainCode", Arguments = new() { ["path"] = file, ["lang"] = lang } });
            plan.Steps.Add(new AgentStep { Description = $"Finde Probleme im Code {file}", SkillName = "CodeSkill", FunctionName = "FindIssues", Arguments = new() { ["path"] = file, ["lang"] = lang } });
            return plan;
        }

        // Fallback 2: PDF-Zusammenfassung retten
        if (file != null && file.EndsWith(".pdf"))
        {
            var wantsSummary = inputLower.Contains("fasse") || inputLower.Contains("zusammen") || inputLower.Contains("summarize");
            plan.Steps.Add(new AgentStep { Description = wantsSummary ? $"Fasse das PDF-Dokument '{file}' zusammen" : $"Lese das PDF-Dokument '{file}'", SkillName = "PdfSkill", FunctionName = wantsSummary ? "SummarizePdf" : "ReadPdf", Arguments = new() { ["path"] = file, ["lang"] = lang } });
            plan.Steps.Add(new AgentStep { Description = "Reflektiere das Gesamtergebnis", SkillName = "ReflectSkill", FunctionName = "Reflect", Arguments = new() { ["input"] = "", ["lang"] = lang } });
            return plan;
        }

        // Fallback 3: Standard-Textdateien (.txt, .md, .json)
        if (file != null && (file.EndsWith(".txt") || file.EndsWith(".md") || file.EndsWith(".json")))
        {
            plan.Steps.Add(new AgentStep { Description = $"Lese den Inhalt der Datei '{file}'", SkillName = "FileSkill", FunctionName = "ReadFile", Arguments = new() { ["path"] = file, ["lang"] = lang } });
            
            if (!inputLower.Contains("speichere") && !inputLower.Contains("memory"))
            {
                plan.Steps.Add(new AgentStep { Description = "Erkläre den Inhalt der Datei", SkillName = "ReflectSkill", FunctionName = "Reflect", Arguments = new() { ["input"] = "", ["lang"] = lang } });
            }
            return plan;
        }

        return plan;
    }

    private static List<JsonElement> ParseJsonToSteps(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("steps", out var stepsProp) && stepsProp.ValueKind == JsonValueKind.Array
            ? stepsProp.EnumerateArray().Select(el => el.Clone()).ToList() 
            : new List<JsonElement>();
    }

    private static AgentStep? ProcessAndFixStep(JsonElement stepElement, string userInput, string forcedSkill, string lang)
    {
        var description = stepElement.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";
        var skillName = stepElement.TryGetProperty("skill", out var skillProp) ? skillProp.GetString() ?? "" : "";
        var functionName = stepElement.TryGetProperty("function", out var funcProp) ? funcProp.GetString() ?? "" : "";

        var step = new AgentStep
        {
            Description = description,
            SkillName = skillName,
            FunctionName = functionName,
            Arguments = new()
        };

        // Argumente-Mapping über System.Text.Json
        if (stepElement.TryGetProperty("arguments", out var argsElement) && argsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var arg in argsElement.EnumerateObject())
            {
                step.Arguments[arg.Name] = arg.Value.ValueKind == JsonValueKind.String 
                    ? arg.Value.GetString() ?? "" 
                    : arg.Value.GetRawText().Trim('"');
            }
        }

        if (string.IsNullOrWhiteSpace(step.FunctionName))
        {
            Console.WriteLine("→ Auto-Fix: Leerer Funktionsname → Step wird entfernt.");
            return null;
        }

        // Edge Case: Llama setzt fälschlicherweise Beschreibung als Funktionsname ein
        if (step.FunctionName.Equals(step.Description, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"→ Auto-Fix: Beschreibung '{step.Description}' fälschlich als Funktion erkannt. Setze auf ReadPdf.");
            step.FunctionName = "ReadPdf";
            step.SkillName = "PdfSkill";
        }

        // PDF-Skill erzwingen falls detektiert
        if (forcedSkill == "PdfSkill" && step.SkillName != "ReflectSkill")
        {
            step.SkillName = "PdfSkill";
            if (string.IsNullOrWhiteSpace(step.FunctionName))
                step.FunctionName = "ReadPdf";
        }

        // Alias-Mapping via PlannerConfig
        var fnLower = step.FunctionName.ToLower();
        if (PlannerConfig.FunctionAliases.TryGetValue(fnLower, out var alias))
        {
            step.SkillName = alias.skill;
            step.FunctionName = alias.function;
        }
        else if (!PlannerConfig.KnownFunctions.ContainsKey(fnLower))
        {
            ResolveHallucinatedFunction(step, userInput, forcedSkill);
        }

        ApplyPathFixes(step, userInput);

        if (!step.Arguments.ContainsKey("lang"))
            step.Arguments["lang"] = lang;

        return step;
    }

    private static void ResolveHallucinatedFunction(AgentStep step, string userInput, string forcedSkill)
    {
        var inputLower = userInput.ToLower();

        if (forcedSkill == "PdfSkill")
        {
            step.SkillName = "PdfSkill";
            step.FunctionName = inputLower.Contains("fasse") || inputLower.Contains("zusammen") || inputLower.Contains("summarize")
                ? "SummarizePdf" 
                : "ReadPdf";
        }
        else if (step.SkillName == "ReflectSkill" || 
                 inputLower.Contains("erkläre") || inputLower.Contains("erklär") || inputLower.Contains("explain") || 
                 inputLower.Contains("summarize") || inputLower.Contains("zusammen") || inputLower.Contains("fasse"))
        {
            step.SkillName = "ReflectSkill";
            step.FunctionName = "Reflect";
            step.Arguments["input"] = step.Arguments.TryGetValue("input", out var existingVal) ? existingVal : step.Description;
        }
        else
        {
            var file = ExtractFileNameFromInput(userInput);
            (step.SkillName, step.FunctionName) = file switch
            {
                _ when file != null && file.EndsWith(".cs") => ("CodeSkill", "ExplainCode"),
                _ when file != null && (file.EndsWith(".txt") || file.EndsWith(".json") || file.EndsWith(".md")) => ("FileSkill", "ReadFile"),
                _ => ("DirectorySkill", "ListDirectory")
            };
        }
    }

    private static void ApplyPathFixes(AgentStep step, string userInput)
    {
        if (!step.Arguments.ContainsKey("path"))
        {
            var file = ExtractFileNameFromInput(userInput);
            if (file != null)
            {
                Console.WriteLine($"[Planner] Auto-Fix: Injiziere fehlenden Pfad '{file}' für {step.SkillName}.{step.FunctionName}");
                step.Arguments["path"] = file;
            }
        }

        if (step.SkillName == "DirectorySkill" && (!step.Arguments.TryGetValue("path", out var path) || string.IsNullOrWhiteSpace(path) || path == "current"))
        {
            Console.WriteLine($"[Planner] Auto-Fix: Injiziere aktuellen Ordner '.' für {step.SkillName}.{step.FunctionName}");
            step.Arguments["path"] = ".";
        }
    }

    private static void ApplyMemoryBooster(AgentPlan plan, string userInput)
    {
        var inputLower = userInput.ToLower();
        if ((inputLower.Contains("memory") || inputLower.Contains("speichere")) && !plan.Steps.Any(s => s.SkillName == "MemorySkill"))
        {
            Console.WriteLine("⚡ NovaMind-Power: Memory-Bedarf erkannt! Injiziere automatischen Speicher-Schritt...");
            
            var category = "general";
            if (inputLower.Contains("kategorie"))
            {
                var words = userInput.Split(' ');
                for (int i = 0; i < words.Length - 1; i++)
                {
                    if (words[i].ToLower().Contains("kategorie"))
                    {
                        category = words[i + 1].Trim(',', '.', ' ');
                        break;
                    }
                }
            }

            var memoryStep = new AgentStep
            {
                Description = $"Speichere das Ergebnis in der Kategorie '{category}'",
                SkillName = "MemorySkill",
                FunctionName = "Remember",
                Arguments = new() { ["input"] = $"{category}: " }
            };
            
            var reflectIdx = plan.Steps.FindIndex(s => s.SkillName == "ReflectSkill" || s.FunctionName == "Reflect");
            if (reflectIdx >= 0)
                plan.Steps.Insert(reflectIdx, memoryStep);
            else
                plan.Steps.Add(memoryStep);
        }
    }

    /// <summary>
    /// Extrahiert mittels LINQ den Dateipfad aus der Eingabe.
    /// </summary>
    private static string? ExtractFileNameFromInput(string input)
    {
        var allowedExtensions = new[] { ".pdf", ".cs", ".txt", ".json", ".md" };
        return input.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(word => word.Contains('.') && allowedExtensions.Any(ext => word.EndsWith(ext)));
    }

    /// <summary>
    /// Bereinigt das LLM-JSON unter Verwendung moderner Range-Operatoren.
    /// </summary>
    private static string SanitizeJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "{}";

        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');

        return start < 0 || end < 0 || end <= start 
            ? "{}" 
            : raw[start..(end + 1)].Trim(); // C# Range Operator (Span-artig & performant)
    }

    /// <summary>
    /// Deklarative Skill-Erkennung mittels C# Switch Expression.
    /// </summary>
    private static string DetectSkill(string input)
    {
        var lower = input.ToLower();
        return lower switch
        {
            _ when lower.Contains(".pdf") || lower.Contains("pdf") => "PdfSkill",
            _ when lower.Contains(".cs") || lower.Contains("code") || lower.Contains("todo") => "CodeSkill",
            _ when lower.Contains("ordner") || lower.Contains("directory") || lower.Contains("folder") => "DirectorySkill",
            _ when lower.Contains("datei") || lower.Contains("file") => "FileSkill",
            _ when lower.Contains("memory") || lower.Contains("merken") => "MemorySkill",
            _ => "Auto"
        };
    }

    private static string BuildSystemPrompt(string forcedSkill)
    {
        var basePrompt = @"
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

        return forcedSkill != "Auto" 
            ? basePrompt + $"\nErzeuge NUR Schritte mit {forcedSkill}, außer der letzte Schritt (ReflectSkill)." 
            : basePrompt;
    }
}