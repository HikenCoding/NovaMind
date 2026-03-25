using Microsoft.SemanticKernel;
using System.IO;

public class FileSkill
{
    [KernelFunction] //This Method is only usable from the AI-Agent NovaMind
    public string ReadFile(string path) //returning path of a file
    {
        if (!File.Exists(path))
        {
            return $"File not found: {path}";
        }

        return File.ReadAllText(path);
    }
}