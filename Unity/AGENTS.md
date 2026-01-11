# AGENTS.md - Unity Game Project Guide

This is a Unity 2022+ game project using HybridCLR for C# hot-reload, YooAsset for asset management, and FairyGUI for UI.

## Build Commands

**No CLI build commands available.** This project uses Unity Editor's built-in build system:
- Build: File > Build Settings in Unity Editor
- Custom build tools: Check `Assets/Editor/Build/` for automation scripts
- Asset bundles: Built via YooAsset window (`Window > YooAsset`)

**Running tests:**
- Unity Test Runner: Window > General > Test Runner
- Run all: Click "Run All" in Test Runner window
- Run single test: Right-click test class/method → "Run"
- Framework: NUnit (via Unity Test Runner package)

## Code Style Guidelines

### Naming Conventions
- **Classes/Structs/Enums**: PascalCase (`UIManager`, `GameState`)
- **Interfaces**: PascalCase with 'I' prefix (`IUI`, `IManager`)
- **Methods**: PascalCase (`ShowUI()`, `HideWindow()`)
- **Properties**: PascalCase (`PlayerName`, `IsActive`)
- **Public fields**: PascalCase (avoid - prefer properties)
- **Private/Protected fields**: camelCase with underscore prefix (`_cache`, `_showing`)
- **Local variables**: camelCase (`playerId`, `itemCount`)
- **Constants**: PascalCase or UPPER_SNAKE_CASE (`MaxPlayers` or `MAX_PLAYERS`)
- **Async methods**: Return `UniTask<T>` or `UniTask` (UniTask vs Task/Task<T>)

### Import Organization
```
1. System namespace imports (alphabetical)
2. Third-party imports (alphabetical)
3. Unity namespace imports (alphabetical)
4. Project namespace imports (alphabetical)
5. static imports (last, if needed)
```

Example:
```csharp
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FairyGUI;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Ux.UIMgr;
```

### Formatting
- **Indentation**: 4 spaces (no tabs)
- **Braces**: K&R style - opening brace on same line
- **Spacing**: Space around binary operators
- **Line endings**: CRLF
- **Line length**: No strict limit, be reasonable (~120 chars preferred)

```csharp
// Correct
public class Example
{
    private int _value;

    public void DoSomething(int x, int y)
    {
        var result = x + y;
        if (result > 0) {
            return;
        }
    }
}
```

### Language Preferences
- Use explicit types (`int` not `var`) when type is not obvious
- Expression-bodied members for properties: `public int Value => _value;`
- NOT for methods: use full method bodies
- Pattern matching preferred over is/cast chains
- Null coalescing: `??` and `?.` operators preferred
- String interpolation: `$"Value: {value}"` preferred

### Error Handling
- Use try-catch sparingly - Unity handles most errors internally
- Log errors using custom `Log.Error()`, `Log.Warning()`, `Log.Debug()`
- For async operations: Pass `CancellationToken` for cancellation
- Return `UniTask<bool>` or similar to indicate success/failure

```csharp
try
{
    var result = await LoadAssetAsync(token);
    return true;
}
catch (OperationCanceledException)
{
    Log.Warning("Asset loading cancelled");
    return false;
}
catch (Exception ex)
{
    Log.Error($"Asset loading failed: {ex.Message}");
    return false;
}
```

### Unity-Specific Patterns
- **Singletons**: Use custom `Singleton<T>` base class
- **UI Components**: Extend `UIBase`, `UIView`, `UIWindow`, or `UITabView`
- **Managers**: Singleton pattern via `SomeMgr : Singleton<SomeMgr>`
- **Event System**: Use custom `EventMgr` with `[Event]` attribute registration
- **Async/Unity**: Use UniTask for all async operations, not Unity Coroutines
- **Resources**: Load via YooAsset `ResMgr`, not built-in `Resources.Load()`
- **Editor-only code**: Wrap in `#if UNITY_EDITOR` blocks

### Code Generation
This project uses code generation:
- UI code: Auto-generated from FairyGUI definitions (`Assets/Hotfix/CodeGen/UI/`)
- Protocol buffers: Auto-generated from `.proto` files (`Assets/Hotfix/CodeGen/Proto/`)
- Config files: Auto-generated from Excel/CSV (`Assets/Hotfix/CodeGen/Config/`)

**DO NOT manually edit generated files.** Changes will be overwritten.

### Architecture Notes
- **Hotfix assembly**: `Assets/Hotfix/` - hot-reloadable code
- **Main assembly**: `Assets/Main/` - non-hot-reloadable core code
- **Editor assembly**: `Assets/Editor/` - editor-only tools
- **ThirdParty**: `Assets/ThirdParty/` - external libraries (UniTask, YooAsset, etc.)
- **HotfixBase**: `Assets/HotfixBase/` - base classes and managers

## Editor Configuration
- `.editorconfig` is present and defines strict rules
- Roslyn analyzers are enabled (see `.csproj` files)
- No StyleCop, Rider, or VS-specific configs found
- Follow the conventions in existing code files when in doubt
