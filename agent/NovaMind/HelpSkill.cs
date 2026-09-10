using System.Text;
using Microsoft.SemanticKernel;

public class HelpSkill 
{
    [KernelFunction]
    public string ShowHelp() //returning all registered Skills
    {
        var sb = new StringBuilder();
        sb.AppendLine("🤖 NovaMind CLI - Verfügbare Befehle:\n");

        sb.AppendLine("📋 ALLGEMEIN");
        sb.AppendLine("  /help                     - Zeigt diese Hilfe an");
        sb.AppendLine("  exit                      - Beendet die Anwendung\n");

        sb.AppendLine("-----\n");

        sb.AppendLine("🤖 AGENT & AUTOMATISIERUNG");
        sb.AppendLine("  /agent <auftrag>          - Führt einen autonomen Multi-Step-Plan aus\n");

        sb.AppendLine("-----\n");

        sb.AppendLine("💻 CODE & MIGRATION");
        sb.AppendLine("  /code explain <pfad>      - Erklärt den Code in einer Datei");
        sb.AppendLine("  /code issues <pfad>       - Findet Fehler und Code-Smells");
        sb.AppendLine("  /code refactor <pfad>     - Optimiert den Code");
        sb.AppendLine("  /code read <pfad>         - Liest Code-Datei ein");
        sb.AppendLine("  /cobol <pfad>             - Migriert C#-Code zu COBOL");
        sb.AppendLine("  /fromcobol <pfad> [lang]  - Migriert COBOL zu C#/Python/Java...\n");

        sb.AppendLine("-----\n");

        sb.AppendLine("📁 DATEI- & SYSTEM-SKILLS");
        sb.AppendLine("  /ls [pfad]                - Listet Dateien und Ordner auf");
        sb.AppendLine("  /readfile <pfad>          - Liest den Inhalt einer Datei");
        sb.AppendLine("  /writefile <pfad> <text>  - Erstellt/Überschreibt eine Datei");
        sb.AppendLine("  /deletefile <pfad>        - Löscht eine Datei\n");

        sb.AppendLine("-----\n");

        sb.AppendLine("📄 PDF-SKILLS");
        sb.AppendLine("  /pdf read <pfad>          - Liest Text aus einer PDF");
        sb.AppendLine("  /pdf search <pfad> <txt>  - Durchsucht eine PDF nach Begriffen");
        sb.AppendLine("  /pdf summary <pfad>       - Erstellt eine KI-Zusammenfassung\n");

        sb.AppendLine("-----\n");

        sb.AppendLine("🧠 MEMORY-SKILLS (Gedächtnis)");
        sb.AppendLine("  /remember <kat: text>     - Speichert Informationen im Langzeitgedächtnis");
        sb.AppendLine("  /memory [kategorie]       - Zeigt gespeicherte Erinnerungen");
        sb.AppendLine("  /searchmemory <begriff>   - Durchsucht das Gedächtnis");
        sb.AppendLine("  /forget <begriff/*>       - Löscht Erinnerungen");

        return sb.ToString();
    }
}