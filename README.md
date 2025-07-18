# 🚀 Lua Scripting API for AvalonInjectLib

![AvalonInjectLib Logo](https://via.placeholder.com/150) *(Replace with actual logo if available)*

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
