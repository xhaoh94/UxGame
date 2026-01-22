# UxGame Agent Guidelines

This document outlines the build processes, code style, and architectural patterns for the UxGame project.
The project is a Unity-based game (Client) with a Go-based Server (binaries provided) and shared Protobuf/Config definitions.

## Project Structure

- `Unity/` - Main Unity project (Client).
- `Proto/` - Protobuf definitions and generation scripts.
- `DataTables/` - Game configuration (Luban) and generation scripts.
- `Server/` - Server executables and configurations.

## Build & Generation Commands

### 1. Code Generation (Run these when modifying definitions)

**Protobuf (Network Messages):**
Run `C:\Project\UxGame\Proto\gen_csharp.bat`
- Inputs: `.proto` files in `Proto/protofiles`
- Outputs: C# files to `Unity/Assets/Hotfix/CodeGen/Proto` (check actual script output if different)

**Game Configuration (Excel/Data):**
Run `C:\Project\UxGame\DataTables\gen.bat`
- Inputs: Excel/Json files in `DataTables/Datas`
- Outputs: C# code and JSON data. Note: You may need to copy the output to `Unity/Assets/Hotfix/CodeGen/Config` and `Unity/Assets/StreamingAssets` if the script doesn't do it automatically.

### 2. Unity Client Build
**No CLI build commands available.** Use Unity Editor:
- **Build Game**: File > Build Settings
- **Build AssetBundles**: Window > YooAsset > AssetBundle Collector/Builder

### 3. Testing
**Unity Test Runner**:
- **Run All**: Window > General > Test Runner > "Run All"
- **Run Single**: Right-click test class/method in Test Runner or Project view → "Run"
- **Framework**: NUnit

## Code Style & Conventions (C# / Unity)

### General Principles
- **Async/Await**: Use `UniTask` and `UniTask<T>` for all async operations. Avoid Unity Coroutines.
- **Hot-Reload**: Most game logic resides in `Assets/Hotfix`. Core framework in `Assets/Main`.
- **Generated Code**: NEVER edit files in `CodeGen/` folders. Edit the source (Proto/Excel) and regenerate.

### Naming
- **Classes/Structs**: `PascalCase` (`UIManager`, `LoginRequest`)
- **Interfaces**: `IPascalCase` (`IWindow`, `IService`)
- **Methods/Properties**: `PascalCase` (`ConnectAsync`, `IsReady`)
- **Fields**:
  - `public`: `PascalCase` (Prefer properties)
  - `private/protected`: `_camelCase` (`_networkClient`, `_isInitialized`)
- **Constants**: `PascalCase` or `UPPER_SNAKE_CASE`

### Formatting & Syntax
- **Indentation**: 4 spaces.
- **Braces**: K&R style (opening brace on same line).
- **Namespaces**:
  ```csharp
  using System;
  using UnityEngine;
  using Cysharp.Threading.Tasks; // Third-party
  using Ux.Proto;               // Project
  ```
- **Null Checking**: Use `?.` and `??` operators.
- **String Interpolation**: Use `$"Key: {value}"` instead of `string.Format`.

### Error Handling
- **Exceptions**: Use sparingly. Prefer returning `UniTask<bool>` or result objects for logic failures.
- **Logging**: Use strict logging wrappers:
  ```csharp
  Log.Debug("Connection started");
  Log.Warning($"Retrying... attempt {count}");
  Log.Error(ex); // Pass exception directly
  ```

## Architecture Patterns

### 1. Managers (Singleton)
Managers should inherit from `Singleton<T>` and handle global state.
```csharp
public class BattleMgr : Singleton<BattleMgr>
{
    public void StartBattle() { ... }
}
```

### 2. Event System
Use the attribute-based event system for decoupling.
```csharp
// Definition
public enum GameEvent { PlayerDied }

// Listener
[Event(GameEvent.PlayerDied)]
private void OnPlayerDied(object args) { ... }

// Trigger
EventMgr.Ins.Run(GameEvent.PlayerDied, playerId);
```

### 3. UI System (FairyGUI)
- Extend `UIWindow` or `UIView`.
- Use `Binder` classes (generated) to access components.
- Life-cycle: `OnInit`, `OnShow`, `OnHide`.

### 4. Resource Loading (YooAsset)
Always load resources asynchronously via `ResMgr`.
```csharp
// Load and Instantiate
GameObject obj = await ResMgr.Ins.InstantiateAsync("Assets/Prefabs/Hero.prefab");
```

## Workflow for Agents
1. **Check Requirements**: Does this task require modifying Proto or Config?
2. **Modify Source**: Edit `.proto` or Excel files first.
3. **Regenerate**: Run the `.bat` scripts.
4. **Implement Logic**: Write C# code in `Assets/Hotfix`.
5. **Verify**: Use `lsp_diagnostics` to check for compile errors.
