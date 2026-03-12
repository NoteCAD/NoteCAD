# NoteCAD - C# Geometric Constraint Solver and CAD Sketcher

NoteCAD is an open-source, parametric CAD application built on the Unity engine with a fully custom geometric constraint solver written in C#. The project was created from scratch — all source code is written in C#.

**Try it online:** http://NoteCAD.online

**Licensing contacts:** contact at notecad dot pro

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

### Prerequisites

- **Unity 6** (version `6000.0.3f1` or compatible). Download Unity Hub from https://unity.com/download and install the matching Unity Editor version.
- **Git** with **Git LFS** support.

### Clone the Repository

The project uses Git submodules for external libraries. Clone with submodules in one step:

```bash
git clone --recurse-submodules https://github.com/NoteCAD/NoteCAD.git
```

If you have already cloned without submodules, initialise them afterwards:

```bash
git submodule update --init --recursive
```

### Open in Unity

1. Launch **Unity Hub**.
2. Click **Add** → **Add project from disk** and select the cloned `NoteCAD` folder.
3. Open the project. Unity will import all assets on the first launch (this may take a few minutes).
4. In the **Project** window, open `Assets/Scenes/NoteCAD.unity`.
5. Press **Play** to run the application inside the editor.

### Build a Standalone / WebGL Binary

1. Open **File → Build Settings**.
2. Select your target platform (e.g. *WebGL*, *Windows*, *Linux*, *macOS*).
3. Click **Switch Platform** if the target is not already active.
4. Click **Build** (or **Build And Run**) and choose an output folder.

---

## Project Structure

```
Assets/
  Code/
    Constraints/   # Geometric constraint implementations
    Entities/      # Sketch entity types (line, arc, circle, …)
    Features/      # 3D feature operations (extrude, revolve, …)
    Solver/        # Algebraic constraint solver (Newton method)
    Tools/         # Editor tools and UI actions
    Behaviours/    # Unity MonoBehaviour helpers
    Geometry/      # Core geometry utilities
    External/      # Third-party libraries (git submodules)
  Scenes/          # Unity scenes (NoteCAD, NoteCAM, SketchEditorExample)
  Prefabs/         # UI and object prefabs
ProjectSettings/   # Unity project settings
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

Contributions, bug reports and feature requests are welcome. Please open an issue or submit a pull request on [GitHub](https://github.com/NoteCAD/NoteCAD).
