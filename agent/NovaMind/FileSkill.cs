using System;
using System.IO;
using System.Text;
using Microsoft.SemanticKernel;

public class FileSkill
{
    [KernelFunction]
    public string ReadFile(string path)
    {
        try
        {
            // Wir lesen direkt. Falls die Datei fehlt oder gesperrt ist,
            // fängt das catch-Block den Fehler sofort sicher ab.
            return File.ReadAllText(path);
        }
        catch (FileNotFoundException)
        {
            return $"❌ Datei nicht gefunden: {path}";
        }
        catch (Exception ex)
        {
            return $"❌ Fehler beim Lesen: {ex.Message}";
        }
    }

    [KernelFunction]
    public string WriteFile(string path, string content)
    {
        try
        {
            File.WriteAllText(path, content);
            return $"💾 Datei erfolgreich geschrieben: {path}";
        }
        catch (Exception ex)
        {
            return $"❌ Fehler beim Schreiben: {ex.Message}";
        }
    }

    [KernelFunction]
    public string ListFiles(string path)
    {
        try
        {
            // Verzeichnisse abrufen
            var files = Directory.GetFiles(path);
            var dirs = Directory.GetDirectories(path);

            // StringBuilder schont den Arbeitsspeicher massiv
            var sb = new StringBuilder();
            
            sb.AppendLine("Ordner:");
            foreach (var d in dirs)
            {
                sb.AppendLine($"- {Path.GetFileName(d)}");
            }

            sb.AppendLine("\nDateien:");
            foreach (var f in files)
            {
                sb.AppendLine($"- {Path.GetFileName(f)}");
            }

            return sb.ToString();
        }
        catch (DirectoryNotFoundException)
        {
            return $"❌ Ordner nicht gefunden: {path}";
        }
        catch (Exception ex)
        {
            return $"❌ Fehler beim Auflisten: {ex.Message}";
        }
    }

    [KernelFunction]
    public string DeleteFile(string path)
    {
        try
        {
            // Direktes Löschen versuchen
            File.Delete(path);
            return $"🗑️ Datei erfolgreich gelöscht: {path}";
        }
        catch (FileNotFoundException)
        {
            return $"❌ Datei existiert nicht: {path}";
        }
        catch (Exception ex)
        {
            return $"❌ Fehler beim Löschen: {ex.Message}";
        }
    }
}