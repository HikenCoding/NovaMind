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
        ["analyze_file_contents"] = ("DirectorySkill", "AnalyzeDirectory"),

        ["summarize"] = ("PdfSkill", "SummarizePdf"),
        ["summarizepdf"] = ("PdfSkill", "SummarizePdf"),
        ["zusammenfassen"] = ("PdfSkill", "SummarizePdf"),
        ["fassezusammen"] = ("PdfSkill", "SummarizePdf")
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
        string inputLower = input.ToLower();
        var file = ExtractFileNameFromInput(input);

        // 🕵️‍♂️ FALLBACK 1: C#-Code-Analyse
        if (inputLower.Contains("analysiere") && file != null && file.EndsWith(".cs"))
        {
            plan.Steps.Add(new AgentStep
            {
                Description = $"Erkläre den Code in {file}",
                SkillName = "CodeSkill",
                FunctionName = "ExplainCode",
                Arguments = new() { ["path"] = file, ["lang"] = lang }
            });

            plan.Steps.Add(new AgentStep
            {
                Description = $"Finde Probleme im Code {file}",
                SkillName = "CodeSkill",
                FunctionName = "FindIssues",
                Arguments = new() { ["path"] = file, ["lang"] = lang }
            });

            return plan;
        }

        // 🕵️‍♂️ FALLBACK 2: PDF-Zusammenfassung retten
        if (file != null && file.EndsWith(".pdf"))
        {
            bool wantsSummary = inputLower.Contains("fasse") || inputLower.Contains("zusammen") || inputLower.Contains("summarize");
            
            plan.Steps.Add(new AgentStep
            {
                Description = wantsSummary ? $"Fasse das PDF-Dokument '{file}' zusammen" : $"Lese das PDF-Dokument '{file}'",
                SkillName = "PdfSkill",
                FunctionName = wantsSummary ? "SummarizePdf" : "ReadPdf",
                Arguments = new() { ["path"] = file, ["lang"] = lang }
            });

            plan.Steps.Add(new AgentStep
            {
                Description = "Reflektiere das Gesamtergebnis",
                SkillName = "ReflectSkill",
                FunctionName = "Reflect",
                Arguments = new() { ["input"] = "", ["lang"] = lang }
            });

            return plan;
        }

        // 🔥 BRANDNEU - FALLBACK 3: Normale Textdateien (.txt, .md, .json) einlesen retten!
        if (file != null && (file.EndsWith(".txt") || file.EndsWith(".md") || file.EndsWith(".json")))
        {
            plan.Steps.Add(new AgentStep
            {
                Description = $"Lese den Inhalt der Datei '{file}'",
                SkillName = "FileSkill",
                FunctionName = "ReadFile",
                Arguments = new() { ["path"] = file, ["lang"] = lang }
            });

            // Wenn der User es nicht explizit im Gedächtnis speichern will, hängen wir eine Reflexion an
            if (!inputLower.Contains("speichere") && !inputLower.Contains("memory"))
            {
                plan.Steps.Add(new AgentStep
                {
                    Description = "Erkläre den Inhalt der Datei",
                    SkillName = "ReflectSkill",
                    FunctionName = "Reflect",
                    Arguments = new() { ["input"] = "", ["lang"] = lang }
                });
            }

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
        // Rohen Tet vom LLM holen
        string json = response.Content ?? "{}";
        json = SanitizeJson(json);

        AgentPlan plan = new AgentPlan { OriginalRequest = input };

        try
        {
            using var doc = JsonDocument.Parse(json);
            var steps = doc.RootElement.GetProperty("steps");

            foreach (var step in steps.EnumerateArray())
            {
                // Sicheres Auslesen mit TryGetProperty statt GetProperty
                string description = step.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";
                string skillName = step.TryGetProperty("skill", out var skillProp) ? skillProp.GetString() ?? "" : "";
                string functionName = step.TryGetProperty("function", out var funcProp) ? funcProp.GetString() ?? "" : "";

                var agentStep = new AgentStep
                {
                    Description = description,
                    SkillName = skillName,
                    FunctionName = functionName,
                    Arguments = new()
                };

                // PDF: Pfad IMMER aus der Eingabe ableiten
                if (forcedSkill == "PdfSkill")
                {
                    var file = ExtractFileNameFromInput(input);
                    if (file != null)
                    {
                        Console.WriteLine($"[Planner] Erzwinge PDF-Pfad: {file}");
                        agentStep.Arguments["path"] = file;
                    }
                }

                // ReflectSkill Absicherung
                if (agentStep.SkillName == "ReflectSkill")
                {
                    agentStep.FunctionName = "Reflect";
                    if (!agentStep.Arguments.ContainsKey("input"))
                        agentStep.Arguments["input"] = agentStep.Description;
                }

                // PATCH: Beschreibung darf NIE Funktionsname sein
                if (agentStep.FunctionName.Equals(agentStep.Description, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"→ Auto-Fix: Beschreibung '{agentStep.Description}' fälschlich als Funktion erkannt. Setze auf ReadPdf.");
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
                        agentStep.FunctionName = (input.ToLower().Contains("fasse") || input.ToLower().Contains("zusammen") || input.ToLower().Contains("summarize")) 
                            ? "SummarizePdf" 
                            : "ReadPdf";
                    }
                    else
                    {
                        var file = ExtractFileNameFromInput(input);
                        if (file != null && file.EndsWith(".cs"))
                        {
                            agentStep.SkillName = "CodeSkill";
                            agentStep.FunctionName = "ExplainCode";
                        }
                        else if (file != null && (file.EndsWith(".txt") || file.EndsWith(".json") || file.EndsWith(".md")))
                        {
                            agentStep.SkillName = "FileSkill";
                            agentStep.FunctionName = "ReadFile";
                        }
                        else
                        {
                            agentStep.SkillName = "DirectorySkill";
                            agentStep.FunctionName = "ListDirectory";
                        }
                    }
                }

                // Argumente sicher auslesen
                if (step.TryGetProperty("arguments", out var argsElement) && argsElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var arg in argsElement.EnumerateObject())
                    {
                        // GetString() gibt null zurück, wenn der Wert kein JSON-String ist (z.B. eine Zahl oder ein Objekt)
                        // RawText ist sicherer, falls Llama mal Zahlen oder Booleans liefert
                        string val = arg.Value.ValueKind == JsonValueKind.String 
                            ? arg.Value.GetString() ?? "" 
                            : arg.Value.GetRawText().Trim('"');
                            
                        agentStep.Arguments[arg.Name] = val;
                    }
                }

                // und das LLM den "path" vergessen hat -> automatisch aus dem User-Input extrahieren!
                if (!agentStep.Arguments.ContainsKey("path"))
                {
                    var file = ExtractFileNameFromInput(input);
                    if (file != null)
                    {
                        Console.WriteLine($"[Planner] Auto-Fix: Injiziere fehlenden Pfad '{file}' für {agentStep.SkillName}.{agentStep.FunctionName}");
                        agentStep.Arguments["path"] = file;
                    }
                }

                if (!agentStep.Arguments.ContainsKey("lang"))
                    agentStep.Arguments["lang"] = lang;

                plan.Steps.Add(agentStep);
            }
       
        }
        catch (Exception ex)
        {
            // Gibt uns im Notfall den genauen Fehlergrund im Terminal aus!
            Console.WriteLine($"⚠️ Fehler beim Parsen des JSON-Plans: {ex.Message}");
            plan = CreateSimplePlan(input, lang);
        }

        // 🔥 BOOSTER-ZONE: Hier unten steht er absolut sicher vor jedem Absturz!
        // Wenn der User "memory" oder "speichere" verlangt, aber noch kein MemorySkill im Plan ist...
        if ((input.ToLower().Contains("memory") || input.ToLower().Contains("speichere")) && 
            !plan.Steps.Any(s => s.SkillName == "MemorySkill"))
        {
            Console.WriteLine("⚡ NovaMind-Power: Memory-Bedarf erkannt! Injiziere automatischen Speicher-Schritt...");
            
            string category = "general";
            if (input.ToLower().Contains("kategorie"))
            {
                var words = input.Split(' ');
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
                Arguments = new()
                {
                    ["input"] = $"{category}: " 
                }
            };
            
            int reflectIdx = plan.Steps.FindIndex(s => s.SkillName == "ReflectSkill" || s.FunctionName == "Reflect");
            if (reflectIdx >= 0)
            {
                plan.Steps.Insert(reflectIdx, memoryStep);
            }
            else
            {
                plan.Steps.Add(memoryStep);
            }
        }

        return plan;
    }

    private static string? ExtractFileNameFromInput(string input)
    {
        foreach (var p in input.Split(" ", StringSplitOptions.RemoveEmptyEntries))
            if (p.Contains(".") && (p.EndsWith(".pdf") || p.EndsWith(".cs") || p.EndsWith(".txt") || p.EndsWith(".json") || p.EndsWith(".md")))
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
