using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;

// Setup Kernel Builder
var builder = Kernel.CreateBuilder();

// Ollama LLM 
builder.AddOllamaChatCompletion(
    modelId: "llama3:latest",
    endpoint: new Uri("http://127.0.0.1:11434")
);

// Standard-Plugins ohne Konstruktor-Abhängigkeiten laden
builder.Plugins.AddFromType<HelpSkill>();
builder.Plugins.AddFromType<FileSkill>();
builder.Plugins.AddFromType<MemorySkill>();
builder.Plugins.AddFromType<PdfSkill>();
builder.Plugins.AddFromType<DirectorySkill>(); // Nur einmal registrieren!

// Kernel final zusammenbauen
var kernel = builder.Build();

// Chat-Service für spätere direkte LLM-Anfragen holen
var chat = kernel.GetRequiredService<IChatCompletionService>();

// --- 2. DEPENDENCY INJECTION (Spezial-Skills registrieren) ---
// Anstatt eines temporären Service-Providers holen wir den Chat-Service 
// direkt aus dem fertigen Kernel und registrieren die Skills nachträglich!
kernel.Plugins.AddFromObject(new CodeSkill(chat));
kernel.Plugins.AddFromObject(new ReflectSkill(chat));

Console.WriteLine("🤖 NovaMind CLI gestartet. Schreib etwas (oder nutze /help):");

string? result = null;
string lang = "de"; // Standard-Sprache: Deutsch

// 3.CLI Loop
while (true)
{
    Console.Write("\nNovaMind> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    // EXIT
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    // Sprache der aktuellen Eingabe erkennen
    lang = LanguageDetector.Detect(input);

    //COBOL
    if (input.StartsWith("/cobol"))
    {
        var path = input.Replace("/cobol", "").Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.WriteLine("⚠️ Bitte gib einen Dateipfad an. Beispiel: /cobol Program.cs");
            continue;
        }

        try 
        {
            var codeSkill = kernel.Plugins["CodeSkill"];
            var function = codeSkill["ToCobol"];
            
            var cobolArgs = new KernelArguments { ["path"] = path };
            Console.WriteLine($"⏳ Analysiere {path} und generiere COBOL-Code...");
            var cobolResult = await kernel.InvokeAsync<string>(function, cobolArgs);
            
            Console.WriteLine("\n📜 Generierter COBOL-Code:\n");
            Console.WriteLine(cobolResult);

            // --- Automatisches Speichern im Ordner 'Cobol' ---
            // 1. Zielordner definieren und erstellen, falls er fehlt
            string targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Cobol");
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // 2. Den reinen Dateinamen ohne Pfad holen (z.B. "Program.cs")
            string originalFileName = Path.GetFileName(path);
            
            // 3. Die Endung durch .cob ersetzen (z.B. "Program.cob")
            string newFileName = Path.ChangeExtension(originalFileName, ".cob");
            string fullTargetFilePath = Path.Combine(targetDirectory, newFileName);

            // 4. Den generierten Code in die Datei schreiben
            await File.WriteAllTextAsync(fullTargetFilePath, cobolResult, Encoding.UTF8);
            
            Console.WriteLine($"\n💾 Datei erfolgreich gespeichert unter: {Path.Combine("Cobol", newFileName)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fehler bei der COBOL-Übersetzung oder beim Speichern: {ex.Message}");
        }
        
        continue;
    }

    try
    {
        // --- BEFEHLS-VERTEILER (Command Router) ---
        if (input.StartsWith('/'))
        {
            await HandleCommandAsync(input);
        }
        else
        {
            // Standard-Verhalten: LLM direkt fragen
            var history = new ChatHistory();
            history.AddSystemMessage(LanguageDetector.GetSystemPrompt(lang));
            history.AddUserMessage(input);

            var response = await chat.GetChatMessageContentAsync(history);
            Console.WriteLine(response.Content);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Fehler bei der Verarbeitung: {ex.Message}");
    }
}

// --- 4. HILFSMETHODE FÜR DIE CLI-BEFEHLE (Saubere Trennung) ---
async Task HandleCommandAsync(string input)
{
    // HELP
    if (input == "/help")
    {
        var help = await kernel.InvokeAsync<string>("HelpSkill", "ShowHelp");
        Console.WriteLine(help);
        return;
    }

    // READFILE
    if (input.StartsWith("/readfile "))
    {
        var path = input.Replace("/readfile ", "").Trim();
        result = await kernel.InvokeAsync<string>("FileSkill", "ReadFile", new() { ["path"] = path });
        Console.WriteLine(result);
        return;
    }

    // WRITEFILE
    if (input.StartsWith("/writefile "))
    {
        var parts = input.Split(' ', 3);
        if (parts.Length < 3)
        {
            Console.WriteLine("❌ Verwendung: /writefile <pfad> <text>");
            return;
        }
        result = await kernel.InvokeAsync<string>("FileSkill", "WriteFile", new() { ["path"] = parts[1], ["content"] = parts[2] });
        Console.WriteLine(result);
        return;
    }

    // LS (Dateien auflisten)
    if (input.StartsWith("/ls"))
    {
        var parts = input.Split(' ', 2);
        var path = parts.Length > 1 ? parts[1] : ".";
        result = await kernel.InvokeAsync<string>("FileSkill", "ListFiles", new() { ["path"] = path });
        Console.WriteLine(result);
        return;
    }

    // DELETEFILE
    if (input.StartsWith("/deletefile "))
    {
        var path = input.Replace("/deletefile ", "").Trim();
        result = await kernel.InvokeAsync<string>("FileSkill", "DeleteFile", new() { ["path"] = path });
        Console.WriteLine(result);
        return;
    }

    // REMEMBER
    if (input.StartsWith("/remember "))
    {
        var text = input.Replace("/remember ", "").Trim();
        result = await kernel.InvokeAsync<string>("MemorySkill", "Remember", new() { ["input"] = text });
        Console.WriteLine(result);
        return;
    }

    // SHOW MEMORY
    if (input.StartsWith("/memory"))
    {
        var parts = input.Split(' ', 2);
        string? category = parts.Length > 1 ? parts[1] : null;
        result = await kernel.InvokeAsync<string>("MemorySkill", "ShowMemory", new() { ["category"] = category });
        Console.WriteLine(result);
        return;
    }

    // FORGET
    if (input.StartsWith("/forget "))
    {
        var text = input.Replace("/forget ", "").Trim();
        result = await kernel.InvokeAsync<string>("MemorySkill", "Forget", new() { ["input"] = text });
        Console.WriteLine(result);
        return;
    }

    // SEARCH MEMORY
    if (input.StartsWith("/searchmemory "))
    {
        var text = input.Replace("/searchmemory ", "").Trim();
        result = await kernel.InvokeAsync<string>("MemorySkill", "SearchMemory", new() { ["text"] = text });
        Console.WriteLine(result);
        return;
    }

    // PDF READ
    if (input.StartsWith("/pdf read "))
    {
        var path = input.Replace("/pdf read ", "").Trim();
        result = await kernel.InvokeAsync<string>("PdfSkill", "ReadPdf", new() { ["path"] = path });
        Console.WriteLine(result);
        return;
    }

    // PDF SEARCH
    if (input.StartsWith("/pdf search "))
    {
        var parts = input.Split(' ', 4);
        if (parts.Length < 4)
        {
            Console.WriteLine("❌ Verwendung: /pdf search <pfad> <suchbegriff>");
            return;
        }
        result = await kernel.InvokeAsync<string>("PdfSkill", "SearchPdf", new() { ["path"] = parts[2], ["search"] = parts[3] });
        Console.WriteLine(result);
        return;
    }

    // PDF SUMMARY
    if (input.StartsWith("/pdf summary "))
    {
        var path = input.Replace("/pdf summary ", "").Trim();
        result = await kernel.InvokeAsync<string>("PdfSkill", "SummarizePdf", new() { ["path"] = path, ["kernel"] = kernel });
        Console.WriteLine(result);
        return;
    }

    // CODE READ
    if (input.StartsWith("/code read "))
    {
        var path = input.Replace("/code read ", "").Trim();
        result = await kernel.InvokeAsync<string>("CodeSkill", "ReadCode", new() { ["path"] = path });
        Console.WriteLine(result);
        return;
    }

    // CODE EXPLAIN
    if (input.StartsWith("/code explain "))
    {
        var path = input.Replace("/code explain ", "").Trim();
        result = await kernel.InvokeAsync<string>("CodeSkill", "ExplainCode", new() { ["path"] = path, ["lang"] = lang });
        Console.WriteLine(result);
        return;
    }

    // CODE ISSUES
    if (input.StartsWith("/code issues "))
    {
        var path = input.Replace("/code issues ", "").Trim();
        result = await kernel.InvokeAsync<string>("CodeSkill", "FindIssues", new() { ["path"] = path, ["lang"] = lang });
        Console.WriteLine(result);
        return;
    }

    // CODE REFACTOR
    if (input.StartsWith("/code refactor "))
    {
        var path = input.Replace("/code refactor ", "").Trim();
        result = await kernel.InvokeAsync<string>("CodeSkill", "RefactorCode", new() { ["path"] = path, ["lang"] = lang });
        Console.WriteLine(result);
        return;
    }

    // AGENT (Planer ausführen)
    if (input.StartsWith("/agent "))
    {
        var request = input.Replace("/agent ", "").Trim();
        var combinedOutput = "";

        var plan = await AgentPlanner.CreateLLMPlanAsync(request, lang, chat);

        if (plan.Steps.Count == 0)
        {
            Console.WriteLine("⚠️ LLM-Plan war leer. Nutze SimplePlan...");
            plan = AgentPlanner.CreateSimplePlan(request, lang);
        }

        Console.WriteLine($"📊 Anzahl der Schritte im Plan: {plan.Steps.Count}");

        foreach (var step in plan.Steps)
        {
            Console.WriteLine($"→ Schritt: {step.Description}");

            if (step.SkillName == "MemorySkill" && step.FunctionName == "Remember")
            {
                if (!combinedOutput.Contains(':') && step.Arguments.ContainsKey("input"))
                {
                    string cat = step.Description.Contains('\'') 
                        ? step.Description.Split('\'')[1] 
                        : "dateien";
                        
                    step.Arguments["input"] = $"{cat}: {combinedOutput}";
                }
            }

            if (step.FunctionName == "Reflect")
            {
                step.Arguments["input"] = combinedOutput;
            }

            var agentArgs = new KernelArguments();
            foreach (var arg in step.Arguments)
            {
                agentArgs[arg.Key] = arg.Value;
            }

            // Aktuellen Kernel injizieren
            agentArgs["kernel"] = kernel; 

            var function = kernel.Plugins[step.SkillName][step.FunctionName];
            result = await kernel.InvokeAsync<string>(function, agentArgs);
            Console.WriteLine(result);

            if (!string.IsNullOrWhiteSpace(result))
            {
                combinedOutput += result + "\n\n";
            }
        }
        return;
    }

    Console.WriteLine("⚠️ Unbekannter Befehl. Nutze /help für eine Liste aller Befehle.");
}