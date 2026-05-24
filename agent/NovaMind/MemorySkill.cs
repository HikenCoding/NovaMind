using Microsoft.SemanticKernel;

public class MemorySkill
{
    private static List<string> _memory = new();

    [KernelFunction]
    public string Remember(string text)
    {
        _memory.Add(text);
        return $"Saved to memory: {text}";
    }

    [KernelFunction]
    public string ShowMemory()
    {
        if (_memory.Count == 0)
            return "Memory is empty.";

        var result = "Stored memory:\n";
        foreach (var item in _memory)
            result += "- " + item + "\n";

        return result;
    }

    [KernelFunction]
    public string Forget(string text)
    {
        if (text == "*")
        {
            _memory.Clear();
            return "Memory cleared.";
        }

        if (_memory.Remove(text))
            return $"Removed from memory: {text}";

        return $"Entry not found: {text}";
    }
}
