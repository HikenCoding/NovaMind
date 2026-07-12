using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Xunit;
using System.Collections.Generic;
using System.Text;

namespace NovaMind.Tests;

// 🤖 Ein minimalistischer Mock für den Semantic Kernel Chat-Dienst
public class MockChatCompletionService : IChatCompletionService
{
    private readonly string _responseToReturn;

    public MockChatCompletionService(string responseToReturn)
    {
        _responseToReturn = responseToReturn;
    }

    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory, 
        PromptExecutionSettings? executionSettings = null, 
        Kernel? kernel = null, 
        CancellationToken cancellationToken = default)
    {
        // Wir simulieren die Antwort des LLMs mit unserem festen JSON
        var message = new ChatMessageContent(AuthorRole.Assistant, _responseToReturn);
        IReadOnlyList<ChatMessageContent> result = new List<ChatMessageContent> { message };
        return Task.FromResult(result);
    }

    // Für Streaming-Tests (wird hier nicht benötigt, muss aber wegen des Interfaces da sein)
    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory, 
        PromptExecutionSettings? executionSettings = null, 
        Kernel? kernel = null, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

public class AgentPlannerTests
{
    [Fact]
    public async Task CreateLLMPlan_ShouldInjectCurrentDirectory_WhenLlmOmitsPath()
    {
        // 1. ARRANGE (Vorbereitung des Testfalls - entspricht TC9)
        string userInput = "liste alle Dateien im aktuellen Ordner auf";
        string targetLanguage = "de";

        // Wir simulieren, dass Llama ein fehlerhaftes JSON liefert, bei dem "arguments" komplett leer ist!
        string fakeLlmJson = @"
        {
          ""steps"": [
            {
              ""description"": ""List all files in the current directory"",
              ""skill"": ""DirectorySkill"",
              ""function"": ""ListDirectory"",
              ""arguments"": {}
            }
          ]
        }";

        var mockChat = new MockChatCompletionService(fakeLlmJson);

        // 2. ACT (Ausführung der zu testenden Logik)
        AgentPlan plan = await AgentPlanner.CreateLLMPlanAsync(userInput, targetLanguage, mockChat);

        // 3. ASSERT (Überprüfung des Ergebnisses auf Herz und Nieren)
        Assert.NotNull(plan);
        Assert.Single(plan.Steps); // Es darf genau ein Schritt generiert worden sein
        
        var step = plan.Steps[0];
        Assert.Equal("DirectorySkill", step.SkillName);
        Assert.Equal("ListDirectory", step.FunctionName);
        
        // 🔥 Hier prüfen wir, ob dein Auto-Fix in AgentPlanner.cs gegriffen hat!
        Assert.True(step.Arguments.ContainsKey("path"), "Der 'path'-Parameter fehlt im resultierenden Plan!");
        Assert.Equal(".", step.Arguments["path"]); // Muss auf das aktuelle Verzeichnis zeigen!
        Assert.Equal("de", step.Arguments["lang"]); // Sprache muss korrekt mitgeschleift werden
    }
}