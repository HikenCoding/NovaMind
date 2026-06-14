# 🧠 NovaMind - Loakler KI-Agent CLI (C# + Semantic Kernel + Ollama)

NovaMind ist ein modularer, erweiterbarer KI‑Agent, der Dateien liest, Code analysiert, PDFs zusammenfasst, Memory speichert, Pläne erstellt und mehr.
Das Projekt basiert auf Semantic Kernel, Ollama und einem selbst entwickelten LLM‑Planner, der natürliche Sprache in ausführbare Schritte übersetzt.

NovaMind läuft vollständig lokal und benötigt aktuell keine Cloud‑Dienste.


# ✅ Aktuelle Features 

# 📅 LLM‑Planner (AgentPlanner)
- Wandelt natürliche Sprache in strukturierte JSON‑Pläne um
- Führt mehrere Schritte automatisch aus
- Erkennt Dateien, Skills und Funktionen
- Repariert fehlerhafte LLM‑Ausgaben (Validator + Auto‑Fix)
- Hängt automatisch einen Reflexions‑Schritt an
- JSON‑Sanitizer entfernt Text vor/nach dem JSON

# 🛠️ Skills (Plugins)
NovaMind besitzt mehrere modulare Skills:
- FileSkill -> Dateien lesen, schreiben, löschen, auflisten
- PdfSkill ->	PDFs lesen, durchsuchen, zusammenfassen
- CodeSkill ->	Code erklären, TODOs finden, refactoren
- MemorySkill	-> Informationen speichern, suchen, löschen
- ReflectSkill ->	Reflektiert das Gesamtergebnis eines Agent‑Plans
- HelpSkill ->	Zeigt alle verfügbaren Befehle


# 🤖 Agent‑Modus (/agent)
Der Agent‑Modus ist das Herzstück des Projekts:
- Nutzer gibt einen Befehl ein
- LLM erstellt einen Plan
- Validator korrigiert Fehler
- Agent führt jeden Schritt aus
- Ergebnisse werden gesammelt
- Am Ende reflektiert der Agent das Gesamtergebnis 

Beispiel (Im CLI unter dem Projektordner navigieren, dann mit "dotnet run" starten)
/agent speichere die TODOs aus Program.cs im Memory

Der Agent erkennt automatisch:
1. Datei lesen
2. TODOs extrahieren
3. Memory speichern
4. Reflexion

# 🧩 Projektarchitektur

NovaMind
│
├── Program.cs
│   → CLI, Kernel, Skills, Agent‑Loop
│
├── AgentPlanner.cs
│   → LLM‑Planner, JSON‑Sanitizer, Validator, Auto‑Fix
│
├── Skills/
│   ├── FileSkill.cs
│   ├── PdfSkill.cs
│   ├── CodeSkill.cs
│   ├── MemorySkill.cs
│   ├── ReflectSkill.cs
│   └── HelpSkill.cs
│
└── Models/
    ├── AgentPlan.cs
    └── AgentStep.cs



# 🔌 Wie NovaMind funktioniert

1. Program.cs (Das Kontrollzentrum)
Program.cs:
- baut den Kernel
- registriert alle Skills
- startet die CLI
- erkennt Befehle wie /pdf, /code, /memory
- führt Skills direkt aus
- oder startet den Agent‑Modus

Der Agent-Modus:
1. /agent <text> wird eingegeben
2. Program.cs ruft AgentPlanner.CreateLLMPlanAsync() auf
3. Der Plan wird Schritt für Schritt ausgeführt
4. Ergebnisse werden gesammelt
5. ReflectSkill bewertet das Gesamtergebnis


2. AgentPlanner (Der Kopf des Agenten)
Der Planner ist das Herzstück des Systems.
Er übernimmt:
## 📅 LLM-Planung
Er erstellt aus natürlicher Sprache einen JSON-Plan:

{
  "steps": [
{
      "description": "PDF zusammenfassen",
      "skill": "PdfSkill",
      "function": "SummarizePdf",
      "arguments": { "path": "rechnung.pdf" }
    }
  ]
}

# JSON-Sanitizer
Entfernt Text vor "{" und nach "}".

# Validator
Korrigiert falsche Skill/Funktions-Kombinationen:
- LLM erzeugt: CodeSkill.ReadFile
- Validator korrigiert zu: FileSkill.ReadFile

# Auto-Fix
Ergänzt fehlende Argumente:
- path → automatisch aus User‑Input extrahiert
- lang → automatisch gesetzt

# Reflect-Step
Jeder Plan endet mit: 
ReflectSkill.Reflect


🧩 3. Skills (Die Werkzeuge des Agenten)
📁 FileSkill
- ReadFile(path)
- WriteFile(path, content)
- ListFiles(path)
- DeleteFile(path)

📄 PdfSkill
- ReadPdf(path)
- SearchPdf(path, search)
- SummarizePdf(path)

💻 CodeSkill
- ReadCode(path)
- ExplainCode(path, lang)
- FindIssues(path, lang)
- RefactorCode(path, lang)

🔮 MemorySkill
- Remember(input)
- ShowMemory(category)
- SearchMemory(text)
- Forget(input)

🔍 ReflectSkill
- Reflect(input) → fasst das Gesamtergebnis zusammen

❓ HelpSkill
- ShowHelp()


# 🧪 Beispiele
- PDF zusammenfassen
/agent fasse rechnung.pdf zusammen

- TODOs extrahieren und speichern
/agent speichere die TODOs aus Program.cs im Memory

- Code erklären
/agent erkläre Program.cs

- Ordner analysieren
/agent analysiere alle Dateien im src Ordner

# 🔧 Voraussetzungen
- .NET 8
- Ubuntu 22.04 (WSL2)
- Ollama installiert
- Modell: llama3:latest
- Semantic Kernel 1.x

# ▶️ Starten
dotnet run

 # 🏗️ Architecture Overview
# 🐧Ubuntu (WSL2)
Isolierte Entwicklungsumgebung
- Hält Windows vollständig privat und unangetastet
- Hostet alle KI‑Komponenten getrennt vom Hauptsystem

# 🦙 Ollama
Lokale KI‑Engine
- Führt Modelle wie "llama3:latest" direkt auf deinem Rechner aus
- bietet schnelle und vollständige antworten offline

# 👨🏽‍💻 Semantic Kernel
Framework zur Agenten‑Orchestrierung
- Verbindet deinen C#‑Code mit dem KI‑Modell
- Ermöglicht Tools/Skills, Memory, Planung und mehrstufiges Reasoning


# 🌠 Zukunftsideen:
-Web UI erstellen
-Datenbank hinzufügen (Open Source)
-Containisieren
-In eine Cloud (Open Source) deployen



Viel Spaß beim Lesen und ausprobieren! 🔥🤖






