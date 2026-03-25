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
    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.ToLower() == "exit")
        break;

    var result = await kernel.InvokePromptAsync(input);
    Console.WriteLine(result);
}
