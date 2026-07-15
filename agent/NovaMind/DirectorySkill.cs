using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

/// <summary>
/// Skill zur Interaktion mit dem Dateisystem und Verzeichnisstrukturen.
/// </summary>
public class DirectorySkill
{
    /// <summary>
    /// Listet alle Dateien innerhalb eines Verzeichnisses (inkl. Unterverzeichnisse) sicher auf.
    /// </summary>
    [KernelFunction]
    public string ListDirectory(string path)
    {
        try
        {
            // Verhindert relative Pfad-Verwirrungen
            var fullPath = Path.GetFullPath(path);

            if (!Directory.Exists(fullPath))
                return $"❌ Ordner nicht gefunden: {path}";

            var files = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories);

            if (files.Length == 0)
                return $"📁 Ordner ist leer: {path}";

            var sb = new StringBuilder();
            sb.AppendLine("📄 Gefundene Dateien:");
            foreach (var file in files)
            {
                // Zeigt nur den relativen Pfad ab dem gesuchten Ordner an, um die LLM-Kontext-Tokens zu schonen
                var relativePath = Path.GetRelativePath(fullPath, file);
                sb.AppendLine($"- {relativePath}");
            }

            return sb.ToString();
        }
        catch (UnauthorizedAccessException)
        {
            return $"❌ Keine Berechtigung, um auf den Ordner zuzugreifen: {path}";
        }
        catch (Exception ex)
        {
            return $"❌ Fehler beim Lesen des Ordners '{path}': {ex.Message}";
        }
    }

    /// <summary>
    /// Analysiert alle C#-Dateien im Verzeichnis, indem es Code-Erklärungen und Issues generiert.
    /// </summary>
    [KernelFunction]
    public async Task<string> AnalyzeDirectory(string path, Kernel kernel, string lang = "de") // 🚀 Dynamische Zielsprache mit Fallback
    {
        try
        {
            var fullPath = Path.GetFullPath(path);

            if (!Directory.Exists(fullPath))
                return $"❌ Ordner nicht gefunden: {path}";

            var files = Directory.GetFiles(fullPath, "*.cs", SearchOption.AllDirectories);

            if (files.Length == 0)
                return $"📁 Keine .cs Dateien im Ordner: {path}";

            var sb = new StringBuilder();

            foreach (var file in files)
            {
                sb.AppendLine("====================================================");
                sb.AppendLine($"📄 Datei: {Path.GetFileName(file)}"); // Schönerer Header
                sb.AppendLine("====================================================\n");

                var args = new KernelArguments
                {
                    ["path"] = file,
                    ["lang"] = lang
                };

                try
                {
                    // Code erklären
                    var explain = await kernel.InvokeAsync<string>("CodeSkill", "ExplainCode", args);
                    sb.AppendLine("🧠 Erklärung:");
                    sb.AppendLine((explain ?? "Keine Erklärung generiert.") + "\n");

                    // Probleme finden
                    var issues = await kernel.InvokeAsync<string>("CodeSkill", "FindIssues", args);
                    sb.AppendLine("⚠️ Probleme / TODOs:");
                    sb.AppendLine((issues ?? "Keine Probleme gefunden.") + "\n");
                }
                catch (KernelException ex)
                {
                    sb.AppendLine($"⚠️ Fehler beim Aufruf der Code-Analyse für {Path.GetFileName(file)}: {ex.Message}\n");
                }
            }

            return sb.ToString();
        }
        catch (UnauthorizedAccessException)
        {
            return $"❌ Keine Berechtigung für den Zugriff auf den Ordner: {path}";
        }
        catch (Exception ex)
        {
            return $"❌ Fehler bei der Verzeichnisanalyse von '{path}': {ex.Message}";
        }
    }
}