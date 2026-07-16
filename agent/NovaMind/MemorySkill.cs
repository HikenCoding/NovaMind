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
    
    // Lock-Objekt zur Absicherung gegen Multithreading-Kollisionen
    private static readonly object LockObject = new();
    
    private static Dictionary<string, Dictionary<string, string>> _memory = new(StringComparer.OrdinalIgnoreCase);

    public MemorySkill()
    {
        LoadMemory();
    }

    private static void SaveMemory()
    {
        lock (LockObject)
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
                    _memory = new Dictionary<string, Dictionary<string, string>>(data, StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (JsonException)
            {
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
                return $"Saved under category '{category}': {value}";
            }

            // Format 2: Schlüssel=Wert (Standard: general)
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
                return $"Saved key-value: {key} = {value}";
            }

            return "Invalid format. Use 'category: text' or 'key=value'.";
        }
    }

    [KernelFunction]
    public string ShowMemory(string? category = null)
    {
        lock (LockObject)
        {
            if (_memory.Count == 0)
                return "Memory is empty.";

            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(category))
            {
                if (!_memory.TryGetValue(category, out var catDict) || catDict.Count == 0)
                    return $"No entries in category '{category}'.";

                sb.AppendLine($"Memory in category '{category}':");
                foreach (var kv in catDict)
                {
                    sb.AppendLine($"- {kv.Key}: {kv.Value}");
                }
                return sb.ToString();
            }

            sb.AppendLine("All memory:");
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
            return "❌ Kein Suchbegriff angegeben.";

        lock (LockObject)
        {
            if (input == "*")
            {
                _memory.Clear();
                SaveMemory();
                return "Memory cleared.";
            }

            if (input.Contains(':'))
            {
                var parts = input.Split(':', 2);
                var category = parts[0].Trim();
                var value = parts[1].Trim();

                if (!_memory.TryGetValue(category, out var catDict))
                    return $"Category '{category}' not found.";

                var entry = catDict.FirstOrDefault(kv => kv.Value.Equals(value, StringComparison.OrdinalIgnoreCase));

                if (entry.Key == null)
                    return $"Entry not found in '{category}'.";

                catDict.Remove(entry.Key);

                if (catDict.Count == 0)
                    _memory.Remove(category);

                SaveMemory();
                return $"Removed from '{category}': {value}";
            }

            if (input.Contains('='))
            {
                var parts = input.Split('=', 2);
                var key = parts[0].Trim();

                if (!_memory.TryGetValue("general", out var generalDict))
                    return "No general memory found.";

                if (generalDict.Remove(key))
                {
                    if (generalDict.Count == 0)
                        _memory.Remove("general");

                    SaveMemory();
                    return $"Removed key '{key}'.";
                }
                return $"Key '{key}' not found.";
            }

            if (_memory.TryGetValue("general", out var genDict) && genDict.Remove(input))
            {
                if (genDict.Count == 0)
                    _memory.Remove("general");

                SaveMemory();
                return $"Removed key '{input}'.";
            }

            return "Invalid format.";
        }
    }

    [KernelFunction]
    public string SearchMemory(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "❌ Kein Suchbegriff angegeben.";

        lock (LockObject)
        {
            var results = new List<string>();

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
                return $"No memory entries found containing '{text}'.";

            var sb = new StringBuilder();
            sb.AppendLine("Search results:");
            foreach (var result in results)
            {
                sb.AppendLine(result);
            }

            return sb.ToString();
        }
    }
}