using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;

var builder = Kernel.CreateBuilder();

// Ollama LLM einbinden
builder.AddOllamaTextGeneration(
    modelId: "llama3:latest",
    endpoint: new Uri("http://127.0.0.1:11434")
);

builder.Plugins.AddFromType<HelpSkill>(); //Semantic Kernel is loading the class and NovaMind know the function 'ShowHelp'
builder.Plugins.AddFromType<FileSkill>(); // Semantic Kernel is loading the class and NovaMind know the function 'ReadFile'
builder.Plugins.AddFromType<MemorySkill>();


var kernel = builder.Build();

Console.WriteLine("NovaMind CLI gestartet. Schreib etwas:");

while (true)
{
    Console.Write("NovaMind> ");
    var input = Console.ReadLine();

    //HELP
    if (input == "/help")
    {
        var help = kernel.InvokeAsync<string>("HelpSkill", "ShowHelp");
        Console.WriteLine(await help);
        continue;
    }
    //READFILE
    if (input.StartsWith("/readfile "))
    {
        var path = input.Replace("/readfile ", "").Trim();
        var fileResult = await kernel.InvokeAsync<string>("FileSkill", "ReadFile", new() { ["path"] = path });
        Console.WriteLine(fileResult);
        continue;
    }

    //WRITEFILE
    if (input.StartsWith("/writefile "))
    {
        var parts = input.Split(" ", 3); // /writefile pfad text
        if (parts.Length < 3)
        {
            Console.WriteLine("Usage: /writefile <path> <text>");
            continue;
        }

        var path = parts[1];
        var content = parts[2];

        var writeResult = await kernel.InvokeAsync<string>(
            "FileSkill",
            "WriteFile",
            new() { ["path"] = path, ["content"] = content }
        );

        Console.WriteLine(writeResult);
        continue;
    }


    //LS
        if (input.StartsWith("/ls"))
    {
        var parts = input.Split(" ", 2);
        var path = parts.Length > 1 ? parts[1] : "."; // Standard: aktuelles Verzeichnis

        var lsResult = await kernel.InvokeAsync<string>(
            "FileSkill",
            "ListFiles",
            new() { ["path"] = path }
        );

        Console.WriteLine(lsResult);
        continue;
    }

    //DELETEFILE
        if (input.StartsWith("/deletefile "))
    {
        var parts = input.Split(" ", 2);
        if (parts.Length < 2)
        {
            Console.WriteLine("Usage: /deletefile <path>");
            continue;
        }

        var path = parts[1];

        var deleteResult = await kernel.InvokeAsync<string>(
            "FileSkill",
            "DeleteFile",
            new() { ["path"] = path }
        );

        Console.WriteLine(deleteResult);
        continue;
    }

    // REMEMBER
    if (input.StartsWith("/remember "))
    {
        var parts = input.Split(" ", 2);
        var text = parts[1];

        var result = await kernel.InvokeAsync<string>(
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

        var result = await kernel.InvokeAsync<string>(
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

        var result = await kernel.InvokeAsync<string>(
            "MemorySkill", "Forget",
            new() { ["input"] = text }
        );

        Console.WriteLine(result);
        continue;
    }

    
    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.ToLower() == "exit")
        break;

    var result = await kernel.InvokePromptAsync(input);
    Console.WriteLine(result);
}
