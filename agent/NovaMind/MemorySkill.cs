using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;

public class MemorySkill
{
    private static readonly string MemoryFilePath = "memory.json";
    
    // Thread-Sicherheit: Verhindert Abstürze bei gleichzeitigen Lese- und Schreibzugriffen
    private static readonly object LockObject = new();
    
    // Struktur: Erste Ebene = Kategorie, Zweite Ebene = Key -> Value
    private static Dictionary<string, Dictionary<string, string>> _memory = new(StringComparer.OrdinalIgnoreCase);

    public MemorySkill()
    {
        LoadMemory();
    }

    private static void SaveMemory()
    {
        lock (LockObject) // Absichern vor Multithreading-Kollisionen
        {
            try
            {
                var json = JsonSerializer.Serialize(_memory, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(MemoryFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Fehler beim Speichern des Gedächtnisses: {ex.Message}");
            }
        }
    }

    private static void LoadMemory()
    {
        lock (LockObject)
        {
            try
            {
                if (!File.Exists(MemoryFilePath))
                    return;

                var json = File.ReadAllText(MemoryFilePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);

                if (data != null)
                {
                    // Case-insensitive Dictionary für fehlerfreie Suchen erstellen
                    _memory = new Dictionary<string, Dictionary<string, string>>(data, StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (JsonException)
            {
                // Falls die JSON-Datei beschädigt ist, Backup erstellen und leer starten
                Console.WriteLine("⚠️ Gedächtnisdatei beschädigt. Erstelle leeres Gedächtnis.");
                try
                {
                    if (File.Exists(MemoryFilePath))
                        File.Move(MemoryFilePath, $"{MemoryFilePath}.bak", overwrite: true);
                }
                catch { /* ignoriert */ }
                
                _memory = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Fehler beim Laden des Gedächtnisses: {ex.Message}");
            }
        }
    }

    [KernelFunction]
    public string Remember(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "❌ Kein Inhalt zum Merken übergeben.";

        lock (LockObject)
        {
            // Format 1: Kategorie: Wert
            if (input.Contains(':'))
            {
                var parts = input.Split(':', 2);
                var category = parts[0].Trim();
                var value = parts[1].Trim();

                if (!_memory.TryGetValue(category, out var catDict))
                {
                    catDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _memory[category] = catDict;
                }

                var key = $"item{catDict.Count + 1}";
                catDict[key] = value;
                
                SaveMemory();
                return $"💾 Unter Kategorie '{category}' gemerkt: {value}";
            }

            // Format 2: Schlüssel=Wert (Standard-Kategorie: general)
            if (input.Contains('='))
            {
                var parts = input.Split('=', 2);
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (!_memory.TryGetValue("general", out var generalDict))
                {
                    generalDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _memory["general"] = generalDict;
                }

                generalDict[key] = value;

                SaveMemory();
                return $"💾 Schlüssel-Wert gemerkt: {key} = {value}";
            }

            return "❌ Ungültiges Format. Nutze 'Kategorie: Text' oder 'Schlüssel=Wert'.";
        }
    }

    [KernelFunction]
    public string ShowMemory(string? category = null)
    {
        lock (LockObject)
        {
            if (_memory.Count == 0)
                return "📭 Das Gedächtnis ist leer.";

            var sb = new StringBuilder();

            // Bestimmte Kategorie anzeigen
            if (!string.IsNullOrEmpty(category))
            {
                if (!_memory.TryGetValue(category, out var catDict) || catDict.Count == 0)
                    return $"🔍 Keine Einträge in der Kategorie '{category}' gefunden.";

                sb.AppendLine($"🧠 Gedächtnis in Kategorie '{category}':");
                foreach (var kv in catDict)
                {
                    sb.AppendLine($"- {kv.Key}: {kv.Value}");
                }
                return sb.ToString();
            }

            // Gesamtes Gedächtnis (alle Kategorien) anzeigen
            sb.AppendLine("🧠 Gesamtes Gedächtnis:");
            foreach (var cat in _memory)
            {
                sb.AppendLine($"\n[{cat.Key}]");
                foreach (var kv in cat.Value)
                {
                    sb.AppendLine($"- {kv.Key}: {kv.Value}");
                }
            }

            return sb.ToString();
        }
    }

    [KernelFunction]
    public string Forget(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "❌ Kein Suchbegriff zum Vergessen angegeben.";

        lock (LockObject)
        {
            // Komplettes Gedächtnis löschen bei '*'
            if (input == "*")
            {
                _memory.Clear();
                SaveMemory();
                return "🗑️ Komplettes Gedächtnis gelöscht.";
            }

            // Format: Kategorie: Wert
            if (input.Contains(':'))
            {
                var parts = input.Split(':', 2);
                var category = parts[0].Trim();
                var value = parts[1].Trim();

                if (!_memory.TryGetValue(category, out var catDict))
                    return $"🔍 Kategorie '{category}' nicht gefunden.";

                var entry = catDict.FirstOrDefault(kv => kv.Value.Equals(value, StringComparison.OrdinalIgnoreCase));

                if (entry.Key == null)
                    return $"🔍 Eintrag '{value}' in Kategorie '{category}' nicht gefunden.";

                catDict.Remove(entry.Key);
                
                // Leere Kategorien direkt aufräumen
                if (catDict.Count == 0)
                    _memory.Remove(category);

                SaveMemory();
                return $"🗑️ Aus '{category}' gelöscht: {value}";
            }

            // Format: Schlüssel=Wert (Löscht einen Schlüssel aus general)
            if (input.Contains('='))
            {
                var parts = input.Split('=', 2);
                var key = parts[0].Trim();

                if (!_memory.TryGetValue("general", out var generalDict))
                    return "🔍 Keine Einträge unter 'general' gefunden.";

                if (generalDict.Remove(key))
                {
                    if (generalDict.Count == 0)
                        _memory.Remove("general");

                    SaveMemory();
                    return $"🗑️ Schlüssel '{key}' gelöscht.";
                }
                return $"🔍 Schlüssel '{key}' nicht gefunden.";
            }

            // Format: Nur der Schlüssel-Name (Löscht direkt aus general)
            if (_memory.TryGetValue("general", out var genDict) && genDict.Remove(input))
            {
                if (genDict.Count == 0)
                    _memory.Remove("general");

                SaveMemory();
                return $"🗑️ Schlüssel '{input}' gelöscht.";
            }

            return "❌ Ungültiges Format zum Löschen.";
        }
    }

    [KernelFunction]
    public string SearchMemory(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "❌ Kein Suchbegriff übergeben.";

        lock (LockObject)
        {
            var results = new List<string>();

            // Durchsuche alle Kategorien und Werte
            foreach (var category in _memory)
            {
                foreach (var kv in category.Value)
                {
                    if (kv.Key.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                        kv.Value.Contains(text, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add($"[{category.Key}] {kv.Key}: {kv.Value}");
                    }
                }
            }

            if (results.Count == 0)
                return $"🔍 Keine Gedächtniseinträge gefunden, die '{text}' enthalten.";

            var sb = new StringBuilder();
            sb.AppendLine("🔍 Suchergebnisse im Gedächtnis:");
            foreach (var result in results)
            {
                sb.AppendLine(result);
            }

            return sb.ToString();
        }
    }
}