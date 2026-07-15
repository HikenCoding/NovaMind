using System;
using System.IO;
using System.Text;
using Microsoft.SemanticKernel;

/// <summary>
/// Skill für grundlegende Dateioperationen wie Lesen, Schreiben, Auflisten und Löschen.
/// </summary>
public class FileSkill
{
    /// <summary>
    /// Liest den gesamten Inhalt einer Datei sicher ein.
    /// </summary>
    [KernelFunction]
    public string ReadFile(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (FileNotFoundException)
        {
            return $"❌ Datei nicht gefunden: {path}";
        }
        catch (UnauthorizedAccessException)
        {
            return $"❌ Keine Berechtigung zum Lesen der Datei: {path}";
        }
        catch (Exception ex)
        {
            return $"❌ Fehler beim Lesen der Datei '{path}': {ex.Message}";
        }
    }

    /// <summary>
    /// Schreibt oder überschreibt eine Datei mit dem angegebenen Inhalt.
    /// </summary>
    [KernelFunction]
    public string WriteFile(string path, string content)
    {
        try
        {
            // Erstellt automatisch den Ordner-Pfad, falls dieser noch nicht existiert
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, content);
            return $"💾 Datei erfolgreich geschrieben: {path}";
        }
        catch (UnauthorizedAccessException)
        {
            return $"❌ Keine Berechtigung zum Schreiben in die Datei: {path}";
        }
        catch (Exception ex)
        {
            return $"❌ Fehler beim Schreiben der Datei '{path}': {ex.Message}";
        }
    }

    /// <summary>
    /// Listet alle Dateien und Ordner auf der obersten Ebene eines Verzeichnisses auf.
    /// </summary>
    [KernelFunction]
    public string ListFiles(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);

            if (!Directory.Exists(fullPath))
            {
                return $"❌ Verzeichnis nicht gefunden: {path}";
            }

            var dirs = Directory.GetDirectories(fullPath);
            var files = Directory.GetFiles(fullPath);

            // 🚀 Verwende StringBuilder für maximale Performance im Arbeitsspeicher
            var sb = new StringBuilder();
            
            sb.AppendLine("📁 Ordner:");
            if (dirs.Length == 0)
            {
                sb.AppendLine("- (keine Unterordner)");
            }
            else
            {
                foreach (var d in dirs)
                {
                    sb.AppendLine($"- {Path.GetFileName(d)}");
                }
            }

            sb.AppendLine("\n📄 Dateien:");
            if (files.Length == 0)
            {
                sb.AppendLine("- (keine Dateien)");
            }
            else
            {
                foreach (var f in files)
                {
                    sb.AppendLine($"- {Path.GetFileName(f)}");
                }
            }

            return sb.ToString();
        }
        catch (UnauthorizedAccessException)
        {
            return $"❌ Keine Berechtigung, um das Verzeichnis aufzulisten: {path}";
        }
        catch (Exception ex)
        {
            return $"❌ Fehler beim Auflisten des Verzeichnisses '{path}': {ex.Message}";
        }
    }

    /// <summary>
    /// Löscht eine Datei sicher vom Dateisystem.
    /// </summary>
    [KernelFunction]
    public string DeleteFile(string path)
    {
        try
        {
            File.Delete(path); // File.Delete wirft in .NET KEINE Exception, wenn die Datei nicht existiert!
            return $"🗑️ Datei erfolgreich gelöscht: {path}";
        }
        catch (UnauthorizedAccessException)
        {
            return $"❌ Keine Berechtigung zum Löschen der Datei: {path}";
        }
        catch (Exception ex)
        {
            return $"❌ Fehler beim Löschen der Datei '{path}': {ex.Message}";
        }
    }
}