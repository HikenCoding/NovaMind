using Microsoft.SemanticKernel;
using System.Collections.Generic;

public class MemorySkill
{
    private static readonly List<string> _memories = new();

    [KernelFunction]
    public string AddMemory(string text)
    {
        _memories.Add(text);
        return $"Memory added (#{_memories.Count - 1}): {text}";
    }

    [KernelFunction]
    public string GetMemories()
    {
        if (_memories.Count == 0)
            return "No memories stored.";

        var result = "Stored memories:\n";
        for (int i = 0; i < _memories.Count; i++)
            result += $"{i}: {_memories[i]}\n";

        return result;
    }

    [KernelFunction]
    public string DeleteMemory(int index)
    {
        if (index < 0 || index >= _memories.Count)
            return $"Invalid memory index: {index}";

        var removed = _memories[index];
        _memories.RemoveAt(index);

        return $"Deleted memory #{index}: {removed}";
    }
}
