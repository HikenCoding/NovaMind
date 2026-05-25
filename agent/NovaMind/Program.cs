using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;

var builder = Kernel.CreateBuilder();

// Ollama LLM einbinden
builder.AddOllamaTextGeneration(
    modelId: "llama3:latest",
    endpoint: new Uri("http://127.0.0.1:11434")
);

// Skills laden
builder.Plugins.AddFromType<HelpSkill>();
builder.Plugins.AddFromType<FileSkill>();
builder.Plugins.AddFromType<MemorySkill>();

var kernel = builder.Build();

Console.WriteLine("NovaMind CLI gestartet. Schreib etwas:");

// Eine gemeinsame Variable für alle Ergebnisse
string? result = null;

while (true)
{
    Console.Write("NovaMind> ");
    var input = Console.ReadLine();

    if (input is null)
        continue;

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

    // EXIT
    if (input.ToLower() == "exit")
        break;

    if (string.IsNullOrWhiteSpace(input))
        continue;

    // Standard LLM Prompt
    var response = await kernel.InvokePromptAsync(input);
    result = response.GetValue<string>();
    Console.WriteLine(result);

}
