using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Xunit;
using System.Collections.Generic;
using System.Text;

namespace NovaMind.Tests;

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
        // 1. ARRANGE (Vorbereitung des Testfalls)
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
        
        // Hier prüfen wir, ob dein Auto-Fix in AgentPlanner.cs gegriffen hat!
        Assert.True(step.Arguments.ContainsKey("path"), "Der 'path'-Parameter fehlt im resultierenden Plan!");
        Assert.Equal(".", step.Arguments["path"]); // Muss auf das aktuelle Verzeichnis zeigen!
        Assert.Equal("de", step.Arguments["lang"]); // Sprache muss korrekt mitgeschleift werden
    }


    [Fact]
    public async Task CreateLLMPlan_ShouldTriggerSimplePlanFallback_WhenLlmReturnsMalformedJson()
    {
        // 1. ARRANGE (Entspricht JSON-Parser Absturzschutz)
        string userInput = "/agent speichere den Inhalt von test.txt im Memory unter der Kategorie dateien";
        string targetLanguage = "de";

        // Wir simulieren das fehlerhafte JSON, das Llama geliefert hat (ein einzelner Punkt bricht die Syntax)
        string malformedLlmResponse = "⚠️ Fehler beim Parsen... . is an invalid start of a value.";

        var mockChat = new MockChatCompletionService(malformedLlmResponse);

        // 2. ACT
        AgentPlan plan = await AgentPlanner.CreateLLMPlanAsync(userInput, targetLanguage, mockChat);

        // 3. ASSERT
        Assert.NotNull(plan);
        // Da das JSON kaputt war, greift der catch-Block und ruft CreateSimplePlan auf!
        // Der SimplePlan für .txt fügt ReadFile hinzu, und der Booster injiziert den Speicher-Schritt.
        Assert.Equal(2, plan.Steps.Count); 
        
        // Überprüfung von Schritt 1 (ReadFile)
        Assert.Equal("FileSkill", plan.Steps[0].SkillName);
        Assert.Equal("ReadFile", plan.Steps[0].FunctionName);
        Assert.Equal("test.txt", plan.Steps[0].Arguments["path"]);

        // Überprüfung von Schritt 2 (Remember)
        Assert.Equal("MemorySkill", plan.Steps[1].SkillName);
        Assert.Equal("Remember", plan.Steps[1].FunctionName); 
        
        // Verifiziert den 'input'-Key statt 'category'
        Assert.True(plan.Steps[1].Arguments.ContainsKey("input"), "Der 'input'-Parameter fehlt im Memory-Schritt.");
        Assert.Equal("dateien: ", plan.Steps[1].Arguments["input"]);
    }

    [Fact]
    public async Task CreateLLMPlan_ShouldRedirectHallucinatedFollowUpStepsToReflectSkill()
    {
        // 1. ARRANGE (Schutz vor Fantasie-APIs bei Erklärungen)
        string userInput = "summarize test.txt in english";
        string targetLanguage = "en";

        // Wir simulieren, dass das LLM zwar Schritt 1 richtig baut, aber für Schritt 2 APIs erfindet
        string fakeLlmJson = @"
        {
          ""steps"": [
            {
              ""description"": ""Read File"",
              ""skill"": ""FileSkill"",
              ""function"": ""ReadFile"",
              ""arguments"": { ""path"": ""test.txt"" }
            },
            {
              ""description"": ""Convert text to English using Google Translate API"",
              ""skill"": ""Auto"",
              ""function"": ""TranslateUsingGoogleAPI"",
              ""arguments"": {}
            }
          ]
        }";

        var mockChat = new MockChatCompletionService(fakeLlmJson);

        // 2. ACT
        AgentPlan plan = await AgentPlanner.CreateLLMPlanAsync(userInput, targetLanguage, mockChat);

        // 3. ASSERT
        Assert.NotNull(plan);
        Assert.Equal(2, plan.Steps.Count);

        // Schritt 1 bleibt unberührt
        Assert.Equal("FileSkill", plan.Steps[0].SkillName);
        
        // 🔥 Schritt 2: Die Fantasie-API muss vollautomatisch auf ReflectSkill umgebogen worden sein!
        var hijackedStep = plan.Steps[1];
        Assert.Equal("ReflectSkill", hijackedStep.SkillName);
        Assert.Equal("Reflect", hijackedStep.FunctionName);
        Assert.Equal("en", hijackedStep.Arguments["lang"]); // Muss das korrekte Sprach-Argument besitzen
    }


[Fact]
    public async Task CreateLLMPlan_ShouldEnforcePdfSkillAndExtractPath_WhenPdfIsRequested()
    {
        // 1. ARRANGE 
        string userInput = "fasse muster.pdf zusammen";
        string targetLanguage = "de";

        // Wir simulieren ein unvollständiges LLM-JSON, das den Pfad vergessen hat
        string fakeLlmJson = @"
        {
          ""steps"": [
            {
              ""description"": ""Zusammenfassung des Dokuments"",
              ""skill"": ""Auto"",
              ""function"": ""summarize"",
              ""arguments"": {}
            }
          ]
        }";

        var mockChat = new MockChatCompletionService(fakeLlmJson);

        // 2. ACT
        AgentPlan plan = await AgentPlanner.CreateLLMPlanAsync(userInput, targetLanguage, mockChat);

        // 3. ASSERT
        Assert.NotNull(plan);
        Assert.Single(plan.Steps);

        var step = plan.Steps[0];
        // Der Planner muss den Pfad extrahiert und den Skill auf PdfSkill korrigiert haben!
        Assert.Equal("PdfSkill", step.SkillName);
        Assert.Equal("SummarizePdf", step.FunctionName); // 'summarize' Alias wurde aufgelöst
        Assert.Equal("muster.pdf", step.Arguments["path"]);
        Assert.Equal("de", step.Arguments["lang"]);
    }

    [Fact]
    public async Task CreateSimplePlan_ShouldGenerateCodeAnalysisSteps_WhenCsFileIsAnalyzed()
    {
        // 1. ARRANGE
        string userInput = "analysiere Program.cs";
        string targetLanguage = "de";

        // 2. ACT
        // Wir rufen direkt den SimplePlan Fallback auf, der bei Code-Analysen triggert
        AgentPlan plan = AgentPlanner.CreateSimplePlan(userInput, targetLanguage);

        // 3. ASSERT
        Assert.NotNull(plan);
        Assert.Equal(2, plan.Steps.Count); // Muss ExplainCode UND FindIssues enthalten

        // Schritt 1: Code erklären
        Assert.Equal("CodeSkill", plan.Steps[0].SkillName);
        Assert.Equal("ExplainCode", plan.Steps[0].FunctionName);
        Assert.Equal("Program.cs", plan.Steps[0].Arguments["path"]);

        // Schritt 2: Probleme finden
        Assert.Equal("CodeSkill", plan.Steps[1].SkillName);
        Assert.Equal("FindIssues", plan.Steps[1].FunctionName);
        Assert.Equal("Program.cs", plan.Steps[1].Arguments["path"]);
    }

}