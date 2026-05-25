using Microsoft.SemanticKernel;
using System.Text.Json;


public class MemorySkill
{
    //Define datapath
    private static readonly string MemoryFilePath = "memory.json";

    //Constructor to load data
    public MemorySkill()
    {
        LoadMemory();
    }

    private void SaveMemory()
    {
        var json = JsonSerializer.Serialize(_memory, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(MemoryFilePath, json);
    }


    private void LoadMemory()
    {
        if (!File.Exists(MemoryFilePath))
            return;

        var json = File.ReadAllText(MemoryFilePath);

        var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);

        if (data != null)
            _memory = data;
    }


    //first structure : Kategorie
    //second structure: Key->Value
    private static Dictionary<string, Dictionary<string, string>> _memory 
        = new();

    [KernelFunction]
    public string Remember(string input)
    {
        // Format 1: category: value
        // Format 2: key=value (default category: general)

        if (input.Contains(":"))
        {
            var parts = input.Split(":", 2);
            var category = parts[0].Trim();
            var value = parts[1].Trim();

            if (!_memory.ContainsKey(category))
                _memory[category] = new();

            var key = "item" + (_memory[category].Count + 1);
            _memory[category][key] = value;
            SaveMemory();
            return $"Saved under category '{category}': {value}";
        }

        if (input.Contains("="))
        {
            var parts = input.Split("=", 2);
            var key = parts[0].Trim();
            var value = parts[1].Trim();

            if (!_memory.ContainsKey("general"))
                _memory["general"] = new();

            _memory["general"][key] = value;

            SaveMemory();
            return $"Saved key-value: {key} = {value}";
        }
        
        return "Invalid format. Use 'category: text' or 'key=value'.";
    }

    [KernelFunction]
    public string ShowMemory(string? category = null)
    {
        if (_memory.Count == 0)
            return "Memory is empty.";

        if (!string.IsNullOrEmpty(category))
        {
            if (!_memory.ContainsKey(category))
                return $"No entries in category '{category}'.";

            var result = $"Memory in category '{category}':\n";
            foreach (var kv in _memory[category])
                result += $"- {kv.Key}: {kv.Value}\n";

            return result;
        }

        // Show all
        var output = "All memory:\n";
        foreach (var cat in _memory)
        {
            output += $"\n[{cat.Key}]\n";
            foreach (var kv in cat.Value)
                output += $"- {kv.Key}: {kv.Value}\n";
        }
        
        return output;
    }

    [KernelFunction]
public string Forget(string input)
{
    if (input == "*")
    {
        _memory.Clear();
        return "Memory cleared.";
    }

    // Format: category: value
    if (input.Contains(":"))
    {
        var parts = input.Split(":", 2);
        var category = parts[0].Trim();
        var value = parts[1].Trim();

        if (!_memory.ContainsKey(category))
            return $"Category '{category}' not found.";

        var entry = _memory[category]
            .FirstOrDefault(kv => kv.Value == value);

        if (entry.Key == null)
            return $"Entry not found in '{category}'.";

        _memory[category].Remove(entry.Key);
        SaveMemory();
        return $"Removed from '{category}': {value}";

    }

    // Format: key=value
    if (input.Contains("="))
    {
        var parts = input.Split("=", 2);
        var key = parts[0].Trim();

        if (!_memory.ContainsKey("general"))
            return "No general memory found.";

        if (_memory["general"].Remove(key))
        {
            SaveMemory();
            return $"Removed key '{key}'.";
        }
        return $"Key '{key}' not found.";
    }

    // NEW: Format: key (without =)
    if (_memory.ContainsKey("general") && _memory["general"].ContainsKey(input))
    {
        _memory["general"].Remove(input);
        SaveMemory();
        return $"Removed key '{input}'.";
    }

    return "Invalid format.";
}


[KernelFunction]
public string SearchMemory(string text)
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

    return "Search results:\n" + string.Join("\n", results);
}


}