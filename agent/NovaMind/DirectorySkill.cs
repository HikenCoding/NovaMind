using Microsoft.SemanticKernel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class DirectorySkill
{
    // 1) Ordner auflisten
    [KernelFunction]
    public string ListDirectory(string path)
    {
        if (!Directory.Exists(path))
            return $"❌ Ordner nicht gefunden: {path}";

        var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

        if (files.Length == 0)
            return $"📁 Ordner ist leer: {path}";

        return "📄 Gefundene Dateien:\n" + string.Join("\n", files);
    }

    // 2) Ordner analysieren (alle .cs Dateien)
    //    → nutzt CodeSkill.ExplainCode + CodeSkill.FindIssues
    [KernelFunction]
    public async Task<string> AnalyzeDirectory(string path, Kernel kernel)
    {
        if (!Directory.Exists(path))
            return $"❌ Ordner nicht gefunden: {path}";

        var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

        if (files.Length == 0)
            return $"📁 Keine .cs Dateien im Ordner: {path}";

        var sb = new StringBuilder();

        foreach (var file in files)
        {
            sb.AppendLine($"====================================================");
            sb.AppendLine($"📄 Datei: {file}");
            sb.AppendLine($"====================================================\n");

            var args = new KernelArguments
            {
                ["path"] = file,
                ["lang"] = "de"
            };

            // Code erklären
            var explain = await kernel.InvokeAsync<string>("CodeSkill", "ExplainCode", args);
            sb.AppendLine("🧠 Erklärung:");
            sb.AppendLine(explain + "\n");

            // Probleme finden
            var issues = await kernel.InvokeAsync<string>("CodeSkill", "FindIssues", args);
            sb.AppendLine("⚠️ Probleme / TODOs:");
            sb.AppendLine(issues + "\n");
        }

        return sb.ToString();
    }
}
