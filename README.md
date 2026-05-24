# 🧠 NovaMind - Local AI-Agent (C# + Semantic Kernel + Ollama

Its a fully local AI-Agent running inside an isolated Ubuntu (WSL2) environment. So you don't have to worry about deleting anything in your main OS.
It uses Ollama as the local model runtime and Semantic Kernel as the orchestration layer for prompts, agent logic and future tool integrations. This project is part of a personal portfolio to demonstrate modern AI-agent development using open-source technologies, so everyone can do it without spending money for services. The only maybe neccessary product you need is a device like (notebook, or computer) with solid specification because of RAM usage with Ollama for the Modells.


# ✅ Current Features
1. CLI‑Based AI Agent
Fully interactive command‑line interface

Command parsing with fallback to LLM responses
Modular architecture using Semantic Kernel plugins

2. FileSkill (File System Tools)
readfile <path> — Reads and returns file content

writefile <path> <text> — Creates or overwrites files

ls [path] — Lists files and directories

deletefile <path> — Deletes a file

3. MemorySkill (Agent Memory System)
remember <text> — Stores a memory entry
memory — Displays all stored memory entries

forget <text> — Removes a specific entry

forget * — Clears all memory

4. HelpSkill
help — Shows all available commands

5. Local LLM Integration
Uses Ollama as the local model backend

Uses Semantic Kernel for tool execution and orchestration

# 🚀 Planned Features
1. Memory System 2.0
Key‑value memory

Categorized memory

Persistent storage (JSON or local DB)

2. PDF Analysis
Read PDF files

Extract text

Summarize content

Search inside PDFs

3. Code Analysis
Syntax analysis

Error detection

Code explanation

Refactoring suggestions

4. Agent Loop (Planning + Tool Execution)
LLM generates a plan

Executes tools step‑by‑step

Evaluates results

Iterates until the task is complete

5. Optional Web UI
Chat interface

File upload

Tool buttons

Real‑time logs
 

# 🏗️ Architecture Overview
Ubuntu (WSL2)
Isolated development environment
→ Keeps Windows private and untouched
→ Hosts all AI components

Ollama
Local AI engine
→ Runs models like llama3:latest
→ Provides fast, offline inference

Semantic Kernel
Agent orchestration framework
→ Connects C# code with the AI model
→ Enables tools, memory, and multi‑step reasoning

NovaMind (C#/.NET)
Custom CLI agent
→ Sends prompts to Ollama
→ Receives and displays responses
→ Will later support advanced agent capabilities


# 👨🏽‍💻 Requirements
- Ubuntu 22.04 (WSL2)
- .NET 8 SDK
- Ollama installed
- Model: llama3:latest

# 👟 Running the Agent
cd NovaMind
dotnet run

# 🏁 Project Goals
NovaMind aims to evolve into a fully capable local AI agent with:
- Tool execution (file access, system tools, etc.)
- PDF and document analysis
- Memory system
- Multi‑step reasoning (agent loop)
- Web or desktop UI
- Modular architecture for future extensions
This repository documents the entire development journey.



# 🔮 Future plan
Also to add Security Features like: 
-File Hash Scanner (MD5/SHA256)  
-Detect file tampering by generating and comparing cryptographic hashes.

-File Integrity Monitoring  
 -Track file changes using a local hash database and change detection.

-Log Analysis Module  
 -Parse and analyze SSH or web server logs to identify suspicious activity.

-Port Scanner (TCP)  
 -Scan common ports to detect open services and potential attack surfaces.

-Recon Tools  
 -Perform DNS lookups, reverse DNS, and WHOIS queries for basic reconnaissance.

-Basic Malware Pattern Scanner  
 -Identify suspicious strings or known malicious patterns in files.

-Permission Scanner  
 -Check file and directory permissions for insecure configurations.

-Security Automation Integration  
 -Combine multiple modules into automated analysis workflows.


Have fun by reading, trying it out and also getting inspirate 🔥🤖






