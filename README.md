# 🧠 NovaMind - Lokaler KI-Agent für die Kommandozeile (C# + Semantic Kernel + Ollama)

![.NET Core CI](https://github.com/HikenCoding/NovaMind/actions/workflows/ci.yml/badge.svg)

NovaMind ist ein modularer KI-Agent, der vollständig lokal auf dem eigenen Rechner läuft. Das Projekt soll wiederkehrende Aufgaben wie Dateioperationen, Codeanalysen, das Auslesen von PDFs oder ein einfaches Wissensmanagement automatisieren. Alles über eine Kommandozeile (CLI).

Für die Umsetzung kommen **Microsoft Semantic Kernel** und **Ollama (Llama 3)** zum Einsatz. Im Mittelpunkt steht ein selbst entwickelter **Zero-Shot Task-Oriented Agent Planner**, der Eingaben in natürlicher Sprache analysiert und daraus eine Abfolge von Arbeitsschritten erstellt. Da das gesamte System lokal läuft, bleiben alle Daten auf dem eigenen Rechner und es entstehen keine API-Kosten.

---

# ✅ Features

## 📋 Agent Planner (`AgentPlanner.cs`)

Der Agent Planner erstellt aus einer Eingabe einen Ausführungsplan und kümmert sich darum, dass dieser möglichst zuverlässig abgearbeitet werden kann.

- Wandelt natürliche Sprache in mehrstufige JSON-Pläne (`AgentPlan`) um.
- Bereinigt fehlerhafte JSON-Antworten des Sprachmodells (z. B. Markdown-Codeblöcke oder zusätzlichen Text).
- Ergänzt fehlende Parameter automatisch, wenn das Modell sie vergessen hat.
- Erkennt falsche Skill-Zuordnungen und korrigiert sie (z. B. `CodeSkill.ReadFile` → `FileSkill.ReadFile`).
- Übergibt Ergebnisse automatisch an den nächsten Schritt, sodass Informationen während der Ausführung erhalten bleiben.

---

## 🛠️ Skills (Plugins)

Die einzelnen Funktionen sind als unabhängige Skills aufgebaut und werden über das `[KernelFunction]`-Attribut im Semantic Kernel registriert.

### 📁 FileSkill
- Dateien lesen, schreiben, löschen und Verzeichnisse auflisten.

### 📄 PdfSkill
- PDFs auslesen, durchsuchen und zusammenfassen.

### 💻 CodeSkill
- Quellcode analysieren, Code Smells und TODOs finden sowie Verbesserungsvorschläge erstellen.

### 🧠 MemorySkill
- Speichert Informationen dauerhaft in einer JSON-Datei und ermöglicht das Suchen, Anzeigen und Löschen von Einträgen.

### 🔍 ReflectSkill
- Überprüft am Ende eines Agent-Plans das Ergebnis und bewertet die Ausführung.

### ❓ HelpSkill
- Zeigt alle verfügbaren CLI-Befehle an.

---

# 🤖 Agent-Modus (`/agent`)

Wird eine Anfrage mit `/agent` gestartet, läuft sie in mehreren Schritten ab:

1. Der Benutzer gibt eine Aufgabe in natürlicher Sprache ein.
2. Das Sprachmodell erstellt daraus einen Ausführungsplan.
3. Der Planner prüft die JSON-Struktur und ergänzt fehlende Informationen.
4. Anschließend werden alle Schritte nacheinander ausgeführt.
5. Die Ergebnisse werden automatisch an die folgenden Schritte weitergegeben.
6. Zum Schluss überprüft der ReflectSkill das Gesamtergebnis.

## 🧪 Beispiel

Projekt starten:

```bash
dotnet run
```

Danach kann beispielsweise folgender Befehl ausgeführt werden:

```bash
NovaMind> /agent speichere die TODOs aus Program.cs im Memory
```

Intern wird daraus ungefähr folgende Ausführung:

1. `FileSkill.ReadFile` liest den Quellcode ein.
2. `CodeSkill.FindIssues` sucht nach TODOs.
3. `MemorySkill.Remember` speichert die gefundenen Einträge.
4. `ReflectSkill.Reflect` überprüft das Ergebnis.

---

# 🏗️ Architektur

Beim Aufbau von NovaMind war mir wichtig, die einzelnen Komponenten sauber voneinander zu trennen. Dadurch lassen sich neue Skills einfach hinzufügen oder bestehende erweitern.

## 1. Program.cs

`Program.cs` ist der Einstiegspunkt der Anwendung.

Hier werden:

- der Dependency-Injection-Container eingerichtet,
- das Semantic Kernel initialisiert,
- alle Skills registriert,
- die CLI gestartet und
- entschieden, ob ein Befehl direkt ausgeführt oder an den Agent Planner übergeben wird.

Direkte Befehle wie `/readfile` werden sofort ausgeführt. Komplexere Aufgaben übernimmt der Agent Planner.

---

## 2. AgentPlanner

Der Agent Planner bildet die zentrale Logik des Projekts. Er sorgt dafür, dass aus einer Eingabe in natürlicher Sprache ein ausführbarer Plan entsteht.

### JSON-Sanitizer

Sprachmodelle liefern ihre Antworten nicht immer als gültiges JSON zurück. Der Sanitizer entfernt zusätzlichen Text oder Markdown-Codeblöcke und extrahiert nur den eigentlichen JSON-Inhalt.

### Auto-Fix

Falls das Modell wichtige Parameter wie Dateipfade oder die Programmiersprache vergisst, versucht der Planner diese automatisch aus dem aktuellen Kontext zu ergänzen.

### Validator

Gelegentlich verwendet das Sprachmodell falsche Skills oder Funktionsnamen.

Beispiel:

```text
CodeSkill.ReadFile
```

wird automatisch zu

```text
FileSkill.ReadFile
```

korrigiert.

### Reflect-Schritt

Jeder Agent-Plan endet automatisch mit einem Aufruf von `ReflectSkill.Reflect`. Dadurch wird das Ergebnis noch einmal überprüft und kurz bewertet.

---

## 💡 Technische Details

Ein paar technische Entscheidungen, die im Projekt umgesetzt wurden:

### Thread-sicheres Memory

Der Zugriff auf die `memory.json` wird über ein gemeinsames Lock abgesichert. Dadurch können mehrere Threads nicht gleichzeitig dieselbe Datei verändern und Race Conditions werden vermieden.

### Sichere Dateioperationen

Bei Dateioperationen wird nach dem EAFP-Prinzip gearbeitet ("Easier to Ask for Forgiveness than Permission"). Anstatt zuerst mit `File.Exists()` zu prüfen, wird direkt auf die Datei zugegriffen und mögliche Fehler anschließend behandelt. Dadurch werden typische TOCTOU-Probleme vermieden.

### Effiziente Verarbeitung

Beim Durchlaufen größerer Verzeichnisse kommt `StringBuilder` zum Einsatz, um unnötige Speicherallokationen zu vermeiden.

### Modernes C#

Das Projekt nutzt aktuelle Sprachfeatures aus C# 12, beispielsweise Primary Constructors. Dadurch wird der Code übersichtlicher und Boilerplate für Dependency Injection reduziert.

---

# 🛠️ Verfügbare Skills

## 📁 FileSkill

- `ReadFile(path)` – Liest Textdateien.
- `WriteFile(path, content)` – Schreibt Inhalte in Dateien.
- `ListFiles(path)` – Listet Dateien und Ordner auf.
- `DeleteFile(path)` – Löscht Dateien.

---

## 📂 DirectorySkill

- `ListDirectory(path)` – Listet Dateien und Ordner rekursiv auf.
- `AnalyzeDirectory(path, lang)` – Analysiert einen kompletten Ordner und führt automatisch eine Codeanalyse für gefundene C#-Dateien durch.

---

## 📄 PdfSkill

- `ReadPdf(path)` – Liest den Inhalt einer PDF.
- `SearchPdf(path, search)` – Sucht nach Begriffen innerhalb einer PDF.
- `SummarizePdf(path)` – Erstellt eine Zusammenfassung des Inhalts.

---

## 💻 CodeSkill

- `ReadCode(path)` – Liest Quellcode ein.
- `ExplainCode(path, lang)` – Erklärt den Code.
- `FindIssues(path, lang)` – Findet TODOs, Code Smells und mögliche Schwachstellen.
- `RefactorCode(path, lang)` – Erstellt Vorschläge zur Verbesserung des Codes.

---

## 🧠 MemorySkill

- `Remember(input)` – Speichert Informationen dauerhaft.
- `ShowMemory(category)` – Zeigt Einträge einer Kategorie an.
- `SearchMemory(text)` – Durchsucht das gesamte Memory.
- `Forget(input)` – Löscht einzelne Einträge.
