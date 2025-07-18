# 🔥 AvalonInjectLib - Advanced Game Memory Manipulation Library

![.NET 9 AOT](https://img.shields.io/badge/.NET%209-AOT%20Compiled-blueviolet)
![Memory Manipulation](https://img.shields.io/badge/Function-Memory%20Hacking-red)
![Process Injection](https://img.shields.io/badge/Technique-Process%20Injection-important)

A high-performance C# library compiled with **.NET 9 AOT** for game memory manipulation and cheat development, designed for seamless process injection.

## 🛠️ Key Technical Features
- **AOT Native Compilation** - No JIT overhead, reduced footprint
- **Direct Memory Access** - RW operations with pointer arithmetic
- **Process Injection Ready** - Designed for DLL/ManualMap injection
- **Anti-Detection** - Stealth memory operations
- **Lua Scripting Engine** - Runtime modding without recompilation

## 💻 Compatibility
| Feature               | Supported          |
|-----------------------|--------------------|
| Windows x64           | ✅                |
| .NET 9 AOT            | ✅                |
| DLL Injection         | ✅                |
| Manual Mapping        | ✅                |
| Kernel Drivers        | ❌ (User-mode only)|

## 🚀 Getting Started

### Prerequisites
- .NET 9 SDK
- Visual Studio 2022 (or Rider)
- Administrator privileges (for injection)

A powerful Lua scripting interface for game automation and memory manipulation with AvalonInjectLib.

## 📌 Table of Contents
- [Features](#-features)
- [Quick Start](#-quick-start)
- [API Reference](#-api-reference)
- [Examples](#-examples)
- [Contributing](#-contributing)
- [License](#-license)

## ✨ Features
- ✅ Full memory read/write operations
- ✅ Built-in debugging utilities
- ✅ Math and randomization functions
- ✅ Game-specific helper functions
- ✅ Cross-platform compatibility

## 🚀 Quick Start

### Prerequisites
- AvalonInjectLib installed
- Lua scripts enabled in config

### Basic Usage
1. Create a `.lua` file in your scripts folder
2. Use the API functions:
```lua
-- Simple health monitor
local health = Memory.ReadInt(0x7FF00000)
print("Current health: "..tostring(health))
