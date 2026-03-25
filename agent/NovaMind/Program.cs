using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;

var builder = Kernel.CreateBuilder();

// Ollama LLM einbinden
builder.AddOllamaTextGeneration(
    modelId: "llama3:latest",
    endpoint: new Uri("http://127.0.0.1:11434")
);

var kernel = builder.Build();

Console.WriteLine("NovaMind CLI gestartet. Schreib etwas:");

while (true)
{
    Console.Write("NovaMind> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.ToLower() == "exit")
        break;

    var result = await kernel.InvokePromptAsync(input);
    Console.WriteLine(result);
}
