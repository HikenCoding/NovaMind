using Microsoft.SemanticKernel;

public class DirectorySkill
{
    [KernelFunction]
    public string ListDirectory(string path)
    {
        if (!Directory.Exists(path))
            return $"Ordner nicht gefunden: {path}";

        var files = Directory.GetFiles(path);
        return string.Join("\n", files);
    }

    [KernelFunction]
    public async Task<string> AnalyzeDirectory(string path, Kernel kernel)
    {
        if (!Directory.Exists(path))
            return $"Ordner nicht gefunden: {path}";

        var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

        string result = "";

        foreach (var file in files)
        {
            var args = new KernelArguments { ["path"] = file, ["lang"] = "de" };

            var explain = await kernel.InvokeAsync<string>("CodeSkill", "ExplainCode", args);
            var issues = await kernel.InvokeAsync<string>("CodeSkill", "FindIssues", args);

            result += $"### Datei: {file}\n\n";
            result += $"Erklärung:\n{explain}\n\n";
            result += $"Probleme:\n{issues}\n\n";
        }

        return result;
    }
}
