using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;

var builder = Kernel.CreateBuilder();

// Ollama LLM einbinden
builder.AddOllamaChatCompletion(
    modelId: "llama3:latest",
    endpoint: new Uri("http://127.0.0.1:11434")
);

// Skills laden
builder.Plugins.AddFromType<HelpSkill>();
builder.Plugins.AddFromType<FileSkill>();
builder.Plugins.AddFromType<MemorySkill>();
builder.Plugins.AddFromType<PdfSkill>();
builder.Plugins.AddFromType<ReflectSkill>();


// CodeSkill manuell registrieren (wegen Konstruktor)
var sp = builder.Services.BuildServiceProvider();
var codeChatService = sp.GetRequiredService<IChatCompletionService>();
builder.Plugins.AddFromObject(new CodeSkill(codeChatService));

var kernel = builder.Build();

// Chat-Service holen
var chat = kernel.GetRequiredService<IChatCompletionService>();

Console.WriteLine("NovaMind CLI gestartet. Schreib etwas:");

// Gemeinsame Variablen außerhalb der Schleife deklarieren
string? result = null;
string lang = "de"; // Standard auf DE gesetzt

while (true)
{
    Console.Write("NovaMind> ");
    var input = Console.ReadLine();

    if (input is null)
        continue;

    // Sprache bei jedem Input erkennen
    lang = LanguageDetector.Detect(input);

    // HELP
    if (input == "/help")
    {
        var help = kernel.InvokeAsync<string>("HelpSkill", "ShowHelp");
        Console.WriteLine(await help);
        continue;
    }

    // READFILE
    if (input.StartsWith("/readfile "))
    {
        var path = input.Replace("/readfile ", "").Trim();
        result = await kernel.InvokeAsync<string>("FileSkill", "ReadFile", new() { ["path"] = path });
        Console.WriteLine(result);
        continue;
    }

    // WRITEFILE
    if (input.StartsWith("/writefile "))
    {
        var parts = input.Split(" ", 3);
        if (parts.Length < 3)
        {
            Console.WriteLine("Usage: /writefile <path> <text>");
            continue;
        }

        var path = parts[1];
        var content = parts[2];

        result = await kernel.InvokeAsync<string>(
            "FileSkill",
            "WriteFile",
            new() { ["path"] = path, ["content"] = content }
        );

        Console.WriteLine(result);
        continue;
    }

    // LS
    if (input.StartsWith("/ls"))
    {
        var parts = input.Split(" ", 2);
        var path = parts.Length > 1 ? parts[1] : ".";

        result = await kernel.InvokeAsync<string>(
            "FileSkill",
            "ListFiles",
            new() { ["path"] = path }
        );

        Console.WriteLine(result);
        continue;
    }

    // DELETEFILE
    if (input.StartsWith("/deletefile "))
    {
        var parts = input.Split(" ", 2);
        if (parts.Length < 2)
        {
            Console.WriteLine("Usage: /deletefile <path>");
            continue;
        }

        var path = parts[1];

        result = await kernel.InvokeAsync<string>(
            "FileSkill",
            "DeleteFile",
            new() { ["path"] = path }
        );

        Console.WriteLine(result);
        continue;
    }

    // REMEMBER
    if (input.StartsWith("/remember "))
    {
        var parts = input.Split(" ", 2);
        var text = parts[1];

        result = await kernel.InvokeAsync<string>(
            "MemorySkill", "Remember",
            new() { ["input"] = text }
        );

        Console.WriteLine(result);
        continue;
    }

    // MEMORY
    if (input.StartsWith("/memory"))
    {
        var parts = input.Split(" ", 2);
        string? category = parts.Length > 1 ? parts[1] : null;

        result = await kernel.InvokeAsync<string>(
            "MemorySkill", "ShowMemory",
            new() { ["category"] = category }
        );

        Console.WriteLine(result);
        continue;
    }

    // FORGET
    if (input.StartsWith("/forget "))
    {
        var parts = input.Split(" ", 2);
        var text = parts[1];

        result = await kernel.InvokeAsync<string>(
            "MemorySkill", "Forget",
            new() { ["input"] = text }
        );

        Console.WriteLine(result);
        continue;
    }

    // SEARCH MEMORY
    if (input.StartsWith("/searchmemory "))
    {
        var parts = input.Split(" ", 2);
        var text = parts[1];

        result = await kernel.InvokeAsync<string>(
            "MemorySkill", "SearchMemory",
            new() { ["text"] = text }
        );

        Console.WriteLine(result);
        continue;
    }

    // PDF READ
    if (input.StartsWith("/pdf read "))
    {
        var path = input.Replace("/pdf read ", "");

        result = await kernel.InvokeAsync<string>(
            "PdfSkill", "ReadPdf",
            new() { ["path"] = path }
        );

        Console.WriteLine(result);
        continue;
    }

    // PDF SEARCH
    if (input.StartsWith("/pdf search "))
    {
        var parts = input.Split(" ", 4);
        var path = parts[2];
        var text = parts[3];

        result = await kernel.InvokeAsync<string>(
            "PdfSkill", "SearchPdf",
            new() { ["path"] = path, ["search"] = text }
        );

        Console.WriteLine(result);
        continue;
    }

    //PDF SUMMARY
    if (input.StartsWith("/pdf summary "))
    {
        var path = input.Replace("/pdf summary ", "");

        result = await kernel.InvokeAsync<string>(
            "PdfSkill", "SummarizePdf",
            new() { ["path"] = path, ["kernel"] = kernel }
        );

        Console.WriteLine(result);
        continue;
    }

    // CODE READ
    if (input.StartsWith("/code read "))
    {
        var path = input.Replace("/code read ", "").Trim();

        result = await kernel.InvokeAsync<string>(
            "CodeSkill", "ReadCode",
            new() { ["path"] = path }
        );

        Console.WriteLine(result);
        continue;
    }

    // CODE EXPLAIN
    if (input.StartsWith("/code explain "))
    {
        var path = input.Replace("/code explain ", "").Trim();

        result = await kernel.InvokeAsync<string>(
            "CodeSkill", "ExplainCode",
            new() { ["path"] = path, ["lang"] = lang }
        );

        Console.WriteLine(result);
        continue;
    }

    // CODE ISSUES
    if (input.StartsWith("/code issues "))
    {
        var path = input.Replace("/code issues ", "").Trim();

        result = await kernel.InvokeAsync<string>(
            "CodeSkill", "FindIssues",
            new() { ["path"] = path, ["lang"] = lang }
        );

        Console.WriteLine(result);
        continue;
    }

    // CODE REFACTOR
    if (input.StartsWith("/code refactor "))
    {
        var path = input.Replace("/code refactor ", "").Trim();

        result = await kernel.InvokeAsync<string>(
            "CodeSkill", "RefactorCode",
            new() { ["path"] = path, ["lang"] = lang }
        );

        Console.WriteLine(result);
        continue;
    }

    // agent 
if (input.StartsWith("/agent "))
{
    string combinedOutput = "";
    var request = input.Replace("/agent ", "").Trim();

    var plan = await AgentPlanner.CreateLLMPlanAsync(request, lang, chat);

    if (plan.Steps.Count == 0)
    {
        plan = AgentPlanner.CreateSimplePlan(request, lang);
    }

    foreach (var step in plan.Steps)
    {
        Console.WriteLine($"→ Schritt: {step.Description}");

        if (step.SkillName == "MemorySkill" && step.FunctionName == "Remember")
        {
            // Nur das Ergebnis des vorherigen Steps speichern
            step.Arguments["input"] = result ?? "";
        }

        // Reflect-Step bekommt das gesammelte Ergebnis
        if (step.FunctionName == "Reflect")
        {
            step.Arguments["input"] = combinedOutput;
        }

        // Dictionary → KernelArguments
        var agentArgs = new KernelArguments();
        foreach (var arg in step.Arguments)
        {
            agentArgs[arg.Key] = arg.Value;
        }

        // Skill-Funktion holen
        var function = kernel.Plugins[step.SkillName][step.FunctionName];

        // Ausführen
        result = await kernel.InvokeAsync<string>(function, agentArgs);

        // Ausgabe
        Console.WriteLine(result);

        // Ergebnis sammeln (damit der nächste Schritt darauf zugreifen kann)
        if (!string.IsNullOrWhiteSpace(result))
        {
            combinedOutput += result + "\n\n";
        }
    }

    continue;
}


    // EXIT
    if (input.ToLower() == "exit")
        break;

    if (string.IsNullOrWhiteSpace(input))
        continue;


    // Chat-History erstellen
    var history = new ChatHistory();

    // Systemprompt dynamisch setzen
    history.AddSystemMessage(LanguageDetector.GetSystemPrompt(lang));

    // User-Message hinzufügen
    history.AddUserMessage(input);

    // LLM aufrufen
    var response = await chat.GetChatMessageContentAsync(history);

    // Ausgabe
    result = response.Content;
    Console.WriteLine(result);
}