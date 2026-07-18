using System.Collections.Generic;


public static class PlannerConfig
{

    public static readonly Dictionary<string, (string skill, string function)> KnownFunctions = new()
        {
            ["readfile"] = ("FileSkill", "ReadFile"),
            ["writefile"] = ("FileSkill", "WriteFile"),
            ["listfiles"] = ("FileSkill", "ListFiles"),
            ["deletefile"] = ("FileSkill", "DeleteFile"),

            ["readpdf"] = ("PdfSkill", "ReadPdf"),
            ["searchpdf"] = ("PdfSkill", "SearchPdf"),
            ["summarizepdf"] = ("PdfSkill", "SummarizePdf"),

            ["readcode"] = ("CodeSkill", "ReadCode"),
            ["explaincode"] = ("CodeSkill", "ExplainCode"),
            ["findissues"] = ("CodeSkill", "FindIssues"),
            ["refactorcode"] = ("CodeSkill", "RefactorCode"),

            ["remember"] = ("MemorySkill", "Remember"),
            ["forget"] = ("MemorySkill", "Forget"),
            ["search"] = ("MemorySkill", "Search"),

            ["reflect"] = ("ReflectSkill", "Reflect"),

            ["listdirectory"] = ("DirectorySkill", "ListDirectory"),
            ["analyzedirectory"] = ("DirectorySkill", "AnalyzeDirectory"),

            ["cobol"] = ("CodeSkill", "ToCobol"),
            ["tocobol"] = ("CodeSkill", "ToCobol"),
            ["wandleincobol"] = ("CodeSkill", "ToCobol")
        };

        public static readonly Dictionary<string, (string skill, string function)> FunctionAliases = new()
        {
            ["extracttodos"] = ("CodeSkill", "FindIssues"),
            ["extractcomments"] = ("CodeSkill", "FindIssues"),
            ["gettodos"] = ("CodeSkill", "FindIssues"),

            ["loadfile"] = ("FileSkill", "ReadFile"),
            ["openfile"] = ("FileSkill", "ReadFile"),

            ["loadpdf"] = ("PdfSkill", "ReadPdf"),
            ["openpdf"] = ("PdfSkill", "ReadPdf"),
            ["öffnepdf"] = ("PdfSkill", "ReadPdf"),
            ["öffne_pdf"] = ("PdfSkill", "ReadPdf"),
            ["open pdf file"] = ("PdfSkill", "ReadPdf"),
            ["open"] = ("PdfSkill", "ReadPdf"),
            ["öffne"] = ("PdfSkill", "ReadPdf"),

            ["list_files"] = ("DirectorySkill", "ListDirectory"),
            ["listfiles"] = ("DirectorySkill", "ListDirectory"),
            ["listFiles"] = ("DirectorySkill", "ListDirectory"),
            ["gatherfiles"] = ("DirectorySkill", "ListDirectory"),
            ["scanfiles"] = ("DirectorySkill", "ListDirectory"),

            ["analyze_file"] = ("DirectorySkill", "AnalyzeDirectory"),
            ["analyze_files"] = ("DirectorySkill", "AnalyzeDirectory"),
            ["analyze_directory"] = ("DirectorySkill", "AnalyzeDirectory"),
            ["analyze_file_contents"] = ("DirectorySkill", "AnalyzeDirectory"),

            ["summarize"] = ("PdfSkill", "SummarizePdf"),
            ["summarizepdf"] = ("PdfSkill", "SummarizePdf"),
            ["zusammenfassen"] = ("PdfSkill", "SummarizePdf"),
            ["fassezusammen"] = ("PdfSkill", "SummarizePdf")
        };

        public static readonly Dictionary<string, List<string>> ValidSkills = new()
        {
            ["CodeSkill"] = new() { "ExplainCode", "FindIssues", "RefactorCode", "ReadCode" },
            ["PdfSkill"] = new() { "ReadPdf", "SearchPdf", "SummarizePdf" },
            ["MemorySkill"] = new() { "Remember", "Forget", "Search" },
            ["FileSkill"] = new() { "ReadFile", "WriteFile", "ListFiles", "DeleteFile" },
            ["ReflectSkill"] = new() { "Reflect" },
            ["DirectorySkill"] = new() { "ListDirectory", "AnalyzeDirectory" }
        };

}