// NoteCAD Godot Entry Point
//
// This script bootstraps the NoteCAD application inside the Godot engine.
// The core CAD logic (solver, constraints, entities, sketch) lives in the
// NoteCAD.Core library and is fully engine-independent.  This file and the
// accompanying scene tree handle rendering and user interaction using
// Godot's open-source API instead of Unity.
//
// Platforms supported by Godot:
//   • Windows (64-bit)
//   • Linux (64-bit)
//   • WebAssembly (via Godot's "Web" export preset, WASM + JS runtime)

using Godot;

namespace NoteCAD.Godot;

/// <summary>
/// Top-level application node.  Manages the active sketch/detail and
/// wires together input, rendering, and the NoteCAD.Core engine.
/// </summary>
public partial class Main : Node2D
{
    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    public override void _Ready()
    {
        GD.Print("NoteCAD.Godot starting…");

        // Initialise the core CAD engine.
        // The engine-independent NoteCAD.Core library handles all constraint
        // solving; Godot provides rendering and platform services.
        InitialiseCoreEngine();

        GD.Print("NoteCAD.Core engine initialised.");
    }

    public override void _Process(double delta)
    {
        // Per-frame update: relay input events to the active tool and
        // refresh the canvas when the sketch is dirty.
        ProcessInput();
        if (_sketchDirty)
        {
            QueueRedraw();
            _sketchDirty = false;
        }
    }

    public override void _Draw()
    {
        // Delegate actual drawing to the sketch renderer.
        DrawSketch();
    }

    // -----------------------------------------------------------------------
    // Private fields
    // -----------------------------------------------------------------------

    private bool _sketchDirty = true;

    // Active sketch (created by NoteCAD.Core).
    // The sketch holds all entities and constraints; the Godot layer
    // renders them using DrawLine / DrawCircle / DrawArc primitives.
    private Sketch? _sketch;

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private void InitialiseCoreEngine()
    {
        // Create a blank horizontal sketch in the XY plane.
        _sketch = new Sketch();
        _sketchDirty = true;
    }

    private void ProcessInput()
    {
        // TODO: map Godot InputEvent → NoteCAD tool actions.
        // Each tool (LineTool, ArcTool, …) will handle mouse clicks,
        // drags, and keyboard shortcuts, then call
        //   _sketch.MarkDirtySketch()
        // to trigger a redraw.
    }

    private void DrawSketch()
    {
        if (_sketch == null) return;

        // Draw all entities in the sketch using Godot 2D drawing primitives.
        // Godot's _Draw() callback makes DrawLine / DrawCircle etc. available.
        foreach (var entity in _sketch.entities)
        {
            foreach (var segment in entity.segments)
            {
                var pts = new System.Collections.Generic.List<global::UnityEngine.Vector3>(segment);
                for (int i = 1; i < pts.Count; i++)
                {
                    var a = new Vector2(pts[i - 1].x, -pts[i - 1].y);
                    var b = new Vector2(pts[i].x,     -pts[i].y);
                    DrawLine(a * 100, b * 100, Colors.White, 1.5f);
                }
            }
        }
    }
}
