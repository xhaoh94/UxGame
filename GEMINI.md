# UxGame Project Context

## Project Overview

**UxGame** is a comprehensive Unity game development framework integrating **HybridCLR** (code hot update), **YooAsset** (asset hot update), and **FairyGUI** (UI system). It provides a complete workflow for client-server game development, including tools for protocol generation, data table management, and a local development server.

## Directory Structure

*   **`Unity/`**: The main Unity project folder.
    *   `Assets/Main`: AOT (Ahead-of-Time) entry code.
    *   `Assets/Hotfix`: Hot-update logic code.
    *   `Assets/HotfixBase`: Shared logic between AOT and Hotfix.
    *   `FGUIProject`: FairyGUI UI project files.
*   **`Server/`**: Backend server binaries and configuration.
    *   `uxgame.exe`: The main server executable.
    *   `app_*.yaml`: Configuration files for different server nodes (e.g., login, game).
    *   `etcd/`: Service discovery component.
*   **`DataTables/`**: Excel-based game configuration pipeline using **Luban**.
    *   `Datas/`: Excel source files.
    *   `gen.bat`: Script to generate C# code and JSON data.
*   **`Proto/`**: Network protocol definitions and tools.
    *   `protofiles/`: Source `.proto` files.
    *   `gen_csharp.bat` / `gen_go.bat`: Scripts to generate code for Client and Server.

## Development Workflow

### 1. Protocol Management (`Proto/`)
To modify network messages:
1.  Edit files in `Proto/protofiles/`.
2.  Run `gen_csharp.bat` to update Unity client code.
3.  Run `gen_go.bat` to update Server code (if source is available/applicable).

### 2. Data Tables (`DataTables/`)
To modify game configuration:
1.  Edit Excel files in `DataTables/Datas/`.
2.  Run `DataTables/gen.bat`.
3.  Generated code and data will be output to configured paths (check `luban.conf` or the batch file).

### 3. Server (`Server/`)
To run the local server environment:
1.  Ensure `etcd` is running (usually handled by start scripts or run manually from `Server/etcd/`).
2.  Run `Server/start.bat` to launch the server nodes defined in `app_*.yaml`.
3.  Verify logs in `Server/logs/` (if configured) or console output.

### 4. Client (`Unity/`)
To run the game client:
1.  Open `Unity/` folder in Unity Editor (2021.3 LTS+).
2.  **HybridCLR**: Go to `Tools > HybridCLR > Settings` to configure, then `Tools > HybridCLR > Build` to build hot update DLLs.
3.  **YooAsset**: Go to `YooAsset > Settings` for asset bundle configuration.
4.  **FairyGUI**: Use `Tools > UI > Code Generator` after modifying UI in FairyGUI Editor.

## Key Technologies

*   **Engine**: Unity 2021.3 LTS+
*   **Hot Update**: HybridCLR (Code), YooAsset (Assets)
*   **UI**: FairyGUI
*   **Network**: TCP/KCP/WebSocket (Protobuf)
*   **Config**: Luban (Excel to JSON/Binary)
