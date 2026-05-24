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

var kernel = builder.Build();

Console.WriteLine("NovaMind CLI gestartet. Schreib etwas:");

while (true)
{
    Console.Write("NovaMind> ");
    var input = Console.ReadLine();

    if (input == "/help")
    {
        var help = kernel.InvokeAsync<string>("HelpSkill", "ShowHelp");
        Console.WriteLine(await help);
        continue;
    }
    if (input.StartsWith("/readfile "))
    {
        var path = input.Replace("/readfile ", "").Trim();
        var fileResult = await kernel.InvokeAsync<string>("FileSkill", "ReadFile", new() { ["path"] = path });
        Console.WriteLine(fileResult);
        continue;
    }
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


    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.ToLower() == "exit")
        break;

    var result = await kernel.InvokePromptAsync(input);
    Console.WriteLine(result);
}
