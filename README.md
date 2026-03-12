# NoteCAD - C# Geometric Constraint Solver and CAD Sketcher

NoteCAD is an open-source, parametric CAD application with a fully custom
geometric constraint solver written in C#. The project was created from
scratch — all source code is written in C#.

**Try it online:** http://NoteCAD.online

**Licensing contacts:** contact at notecad dot pro

---

## Engine Migration: Unity → Godot 4

The codebase is being migrated from the proprietary **Unity3D** engine to
the open-source **[Godot 4](https://godotengine.org/)** engine. The
migration preserves all existing C# code and supports the same target
platforms:

| Platform | Status |
|---|---|
| **Windows** (64-bit) | ✅ Core library builds; Godot export in progress |
| **Linux** (64-bit)   | ✅ Core library builds; Godot export in progress |
| **WebAssembly**      | ✅ Core library builds; Godot Web export in progress |

### Architecture

The project is split into two layers:

```
NoteCAD.Core/          ← Engine-independent C# library (NEW)
  Assets/Code/         ← All existing CAD source files (unchanged)
  UnityCompat/         ← Unity math-type shim (Vector3, Mathf, …)

NoteCAD.Godot/         ← Godot 4 rendering/UI layer (NEW)
  Main.cs              ← Entry point, wires Godot ↔ NoteCAD.Core
  Main.tscn            ← Godot scene tree

Assets/                ← Legacy Unity project (still functional)
ProjectSettings/       ← Legacy Unity settings
```

**NoteCAD.Core** compiles as a plain .NET 8 library with *no dependency on
any game engine*. A thin compatibility shim (`UnityCompat/`) provides
`Vector3`, `Mathf`, `Matrix4x4` and other Unity math types so the existing
source files compile without modification.

**NoteCAD.Godot** is the new open-source frontend. It references
NoteCAD.Core and uses Godot's rendering, input, and platform APIs instead
of Unity's proprietary equivalents.

---

## Features

### Sketch Entities
- Lines, arcs, circles, ellipses, elliptic arcs
- Splines
- Text and function curves
- Offset entities

### Geometric Constraints
- Coincident points
- Horizontal / Vertical
- Parallel and Perpendicular
- Tangent
- Equal length / Equal value
- Fixed (Fixation)
- Point on entity
- Distance (point–point, point–line, point–circle, line–line, line–circle, circle–circle)
- Length and Diameter
- Angle
- Custom equation constraints

### 3D Features
- Extrusion
- Revolve
- Mesh import

### Import / Export
- Import: DXF, STL, SolveSpace
- Export: STL, G-code

---

## Building Locally

### Option A — NoteCAD.Core (.NET, no engine required)

Build the engine-independent library with the standard .NET SDK:

```bash
# Prerequisites: .NET 8 SDK (https://dotnet.microsoft.com/download)
git clone --recurse-submodules https://github.com/NoteCAD/NoteCAD.git
cd NoteCAD
dotnet build NoteCAD.Core/NoteCAD.Core.csproj
```

### Option B — NoteCAD.Godot (open-source build)

> **Status:** Godot 4 integration is in progress. The project structure is
> in place; full export support will land as the rendering layer is
> implemented.

1. Install **[Godot 4](https://godotengine.org/download)** (version 4.3 or
   later) with **.NET / C# support** enabled.
2. Clone the repository (with submodules).
3. Open `NoteCAD.Godot/` in the Godot editor.
4. Use **Project → Export** to build for Windows, Linux, or Web (WASM).

Automated builds for all platforms run via GitHub Actions on every push
(see [`.github/workflows/build.yml`](.github/workflows/build.yml)).

### Option C — Legacy Unity build

The original Unity project is still fully functional:

- **Unity 6** (version `6000.0.3f1` or compatible). Download Unity Hub from
  https://unity.com/download and install the matching Unity Editor version.
- **Git** with **Git LFS** support.

```bash
git clone --recurse-submodules https://github.com/NoteCAD/NoteCAD.git
```

1. Launch **Unity Hub**.
2. Click **Add** → **Add project from disk** and select the cloned folder.
3. Open `Assets/Scenes/NoteCAD.unity` and press **Play**.
4. Build via **File → Build Settings** for your target platform.

---

## Project Structure

```
NoteCAD.Core/              # Engine-independent C# library (NEW)
  NoteCAD.Core.csproj      # .NET 8 project
  UnityCompat/             # Unity math-type shim (no Unity DLL required)

NoteCAD.Godot/             # Godot 4 open-source frontend (NEW)
  project.godot            # Godot project configuration
  NoteCAD.Godot.csproj     # C# project
  Main.cs / Main.tscn      # Application entry point

Assets/
  Code/
    Constraints/           # Geometric constraint implementations
    Entities/              # Sketch entity types (line, arc, circle, …)
    Features/              # 3D feature operations (extrude, revolve, …)
    Solver/                # Algebraic constraint solver (Newton method)
    Tools/                 # Editor tools and UI actions
    Behaviours/            # Unity MonoBehaviour helpers
    Geometry/              # Core geometry utilities
    External/              # Third-party libraries (git submodules)
  Scenes/                  # Unity scenes (NoteCAD, NoteCAM, …)
  Prefabs/                 # UI and object prefabs
ProjectSettings/           # Unity project settings
```

### External Libraries (submodules)

| Library | Purpose |
|---|---|
| [Csg](https://github.com/NoteCAD/Csg) | Constructive Solid Geometry |
| [geometry3Sharp](https://github.com/NoteCAD/geometry3Sharp) | 3D geometry algorithms |
| [gsSlicer](https://github.com/NoteCAD/gsSlicer) | 3D-printing slicer |
| [gsGCode](https://github.com/NoteCAD/gsGCode) | G-code generation |
| [netDxf](https://github.com/NoteCAD/netDxf) | DXF file import/export |
| [ACadSharp](https://github.com/NoteCAD/ACadSharp) | Additional CAD file support |
| [SharpFont](https://github.com/NoteCAD/SharpFont) | FreeType font rendering |
| [UnityRuntimeInspector](https://github.com/NoteCAD/UnityRuntimeInspector) | In-app runtime inspector |

---

## Contributing

Contributions, bug reports and feature requests are welcome. Please open an
issue or submit a pull request on [GitHub](https://github.com/NoteCAD/NoteCAD).
