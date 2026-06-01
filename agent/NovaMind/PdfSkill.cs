using Microsoft.SemanticKernel;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

public class PdfSkill
{
    // return pdf as a string
    [KernelFunction]
    public string ReadPdf(string path)
    {
        if (!File.Exists(path))
            return $"File not found: {path}";

        var text = ExtractText(path);
        return text;
    }

    //searching for the input and return text you given
    [KernelFunction]
    public string SearchPdf(string path, string search)
    {
        if (!File.Exists(path))
            return $"File not found: {path}";

        var text = ExtractText(path);

        var matches = text
            .Split('\n')
            .Where(line => line.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return $"No matches found for '{search}'.";

        return "Matches:\n" + string.Join("\n", matches);
    }

    //return text -> send it to LLM -> LLM makes a summary
    [KernelFunction]
    public string SummarizePdf(string path, Kernel kernel)
    {
        if (!File.Exists(path))
            return $"File not found: {path}";

        var text = ExtractText(path);

        var prompt = $"Summarize the following PDF content:\n\n{text}";

        var result = kernel.InvokePromptAsync(prompt).Result;

        return result;
    }

    //opens PDF -> read page by page -> returns text as string
    private string ExtractText(string path)
    {
        var result = new System.Text.StringBuilder();

        using (var document = PdfDocument.Open(path))
        {
            foreach (var page in document.GetPages())
            {
                result.AppendLine(page.Text);
            }
        }

        return result.ToString();
    }
}
