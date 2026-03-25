# 🧠 NovaMind - Local AI-Agent (C# + Semantic Kernel + Ollama

Its a fully local AI-Agent running inside an isolated Ubuntu (WSL2) environment. So you don't have to worry about deleting anything in your main OS.
It uses Ollama as the local model runtime and Semantic Kernel as the orchestration layer for prompts, agent logic and future tool integrations. This project is part of a personal portfolio to demonstrate modern AI-agent development using open-source technologies, so everyone can do it without spending money for services. The only maybe neccessary product you need is a device like (notebook, or computer) with solid specification because of RAM usage with Ollama for the Modells.


# 📖 Current Features
- Local AI agent running in the terminal (CLI)
- Fully offline execution inside Ubuntu (WSL2)
- Uses Ollama as the model backend
- Supports the model llama3:latest
- Integrated with Semantic Kernel for:
  - Prompt handling
  - Text generation
  - Extensibility (tools, memory, planning)
 

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



Have fun by reading, trying it out and also getting inspirate 🔥🤖
