# SGL-DevEngine


[![Platform](https://shields.io)](https://microsoft.com)
[![Version]<img width="1329" height="806" alt="Screenshot 2026-06-20 230205" src="https://github.com/user-attachments/assets/38514f48-8fae-4106-aa42-e936c225286f" />


[![License: MIT](https://shields.io)](https://opensource.org)

SGL-DevEngine is a powerful, standalone **Visual Programming Studio and Node-Based Code Editor** built entirely in C# on .NET 8.0. Fully decoupled from heavy third-party game engines, it provides a lightweight, ultra-fast environment to design, compile, and execute complex logic architectures visually—with native, enterprise-grade database drivers integrated right out of the box.

---

## 🚀 Deployment Options

SGL-DevEngine is production-ready and structured for zero-friction deployment on Windows systems. **Requires .NET 8.0 Runtime installed.**

### Option 1: Quick Deploy (Portable)
1. Navigate to the `/Windows Quick Deploy Version/` folder.
2. Launch `SGLDevEngine.Studio.exe`. 
3. Start building graphs immediately. No installation or project compilation required.

### Option 2: Full System Installer
1. Navigate to the `/SGL DevEngine Windows Install Version/` folder.
2. Run the automated installer package to register the IDE as a native desktop application with clean Control Panel uninstallation support.

---

## ⚡ Key Architectural Capabilities

### 1. Engine-Agnostic Freedom
Traditional blueprint and node-based systems are locked behind massive, bloated game engine pipelines (like Unreal or Unity). SGL-DevEngine operates as a lightweight, independent software application requiring only **4 GB of RAM** and **500 MB of disk space**, making visual programming accessible for lightweight desktop utilities, automation scripts, and tools.

### 2. Built-In Enterprise Data Tier
Unlike standard logic graphs that only process vector maths or ticks, SGL-DevEngine features native, embedded database coupling. Build visual logic nodes that read, write, and query live relational databases seamlessly via integrated drivers:
*   **PostgreSQL** (`Npgsql.dll`)
*   **MySQL** (`MySqlConnector.dll`)
*   **SQLite** (`System.Data.SQLite.dll`)

### 3. Decoupled Core Architecture
The engine is split into highly optimized, modular subsystems to ensure runtime stability:
*   `SGLDevEngine.Core.dll`: Manages backend state, memory management, and system-level data services.
*   `SGLDevEngine.BlueprintRuntime.dll`: The core evaluation engine that compiles and routes logical node chains sequentially.
*   `SGLDevEngine.Studio.dll`: A highly responsive UI canvas framework dedicated to fluid node rendering and link layouts.

---

## 📜 The Evolution

SGL-DevEngine represents the next evolution of structural engine design. Built using deep, foundational knowledge gained from developing native Unreal Engine developer tools, this project was created to solve a major software limitation: **the lack of open, standalone visual script compilers.** 

By extracting the layout mechanics of node-based programming and merging it with native database transaction logic, SGL-DevEngine fills the gap between visual artistic scripting and rigid, backend database engineering.

---

## 🛠️ System Requirements

*   **Operating System**: Windows 10 (Build 19041+) or Windows 11.
*   **Runtime**: Microsoft .NET 8.0 Desktop Runtime.
*   **Memory**: 4 GB RAM minimum.
*   **Graphics**: Dedicated GPU recommended for smooth canvas rendering of massive node networks.

---

## 📈 Future Roadmap

*   [ ] Integration of cross-platform compiling targets (Linux / macOS UI assemblies).
*   [ ] Sandbox execution environments to containerize unverified node scripts safely.
*   [ ] Direct API webhooks node layer for real-time cloud data exchange.

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 How to Contribute

This is a fully open-source project, and community contributions are highly welcome! Whether you want to fix a bug, optimize the node rendering pipeline, or add new database node features, here is how you can help:

1. **Fork the Repository**: Create your own copy of the project to work on.
2. **Create a Feature Branch**: Keep your changes organized (e.g., `git checkout -b feature/NewDatabaseNode`).
3. **Commit Your Changes**: Write clear commit messages explaining what you fixed or added.
4. **Open a Pull Request**: Submit your changes back to the main repository for review.

### 💡 Areas Needing Contributions:
* **Custom Nodes**: Building new default logic nodes for everyday coding math or text operations.
* **UI/UX Refinements**: Optimizing zoom, pan, and visual wire connections on large graphs.
* **Testing & Bug Hunts**: Running the compiled `.exe` on different Windows environments and reporting setup crashes.
<img width="1724" height="954" alt="s1" src="https://github.com/user-attachments/assets/b49e0940-2f9b-4277-bbc9-02addfd78830" />
