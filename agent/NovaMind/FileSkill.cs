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

    [KernelFunction]
    public string WriteFile(string path, string content)
    {
        try
        {
            File.WriteAllText(path, content);
            return $"File written successfully: {path}";
        }
        catch (Exception ex)
        {
            return $"Error writing file: {ex.Message}";
        }
    }

    [KernelFunction]
    public string ListFiles(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return $"Directory not found: {path}";
            }

            var files = Directory.GetFiles(path);
            var dirs = Directory.GetDirectories(path);

            var result = "Directories:\n";
            foreach (var d in dirs)
                result += "- " + Path.GetFileName(d) + "\n";

            result += "\nFiles:\n";
            foreach (var f in files)
                result += "- " + Path.GetFileName(f) + "\n";

            return result;
        }
        catch (Exception ex)
        {
            return $"Error listing directory: {ex.Message}";
        }
    }

    [KernelFunction]
    public string DeleteFile(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return $"File not found: {path}";
            }

            File.Delete(path);
            return $"File deleted successfully: {path}";
        }
        catch (Exception ex)
        {
            return $"Error deleting file: {ex.Message}";
        }
    }



}