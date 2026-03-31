// Stubs for Unity-scene and editor-layer types that the core CAD code
// references (e.g. DetailEditor.instance, EntityConfig.instance).
// These stubs return safe default values so the library compiles and the
// constraint solver / sketch engine can run without any Unity scene.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ---------------------------------------------------------------------------
// ICanvas – thin rendering abstraction used by constraints and entities.
// A real engine integration should implement this interface.
// ---------------------------------------------------------------------------
public interface ICanvas
{
    void SetStyle(Style style);
    void DrawLine(Vector3 a, Vector3 b);
    void DrawPoint(Vector3 pt);
}

// ---------------------------------------------------------------------------
// ICanvasExt – extension methods for named styles
// ---------------------------------------------------------------------------
public static class ICanvasExt
{
    private static readonly Dictionary<string, Style> _styleCache = new();

    public static void SetStyle(this ICanvas canvas, string name)
    {
        if (!_styleCache.TryGetValue(name, out var style))
        {
            var stroke = EntityConfig.instance.styles.styles
                .FirstOrDefault(s => s.name == name) ?? new StrokeStyle { name = name };
            style = new Style { stroke = stroke };
            _styleCache[name] = style;
        }
        canvas.SetStyle(style);
    }
}

// ---------------------------------------------------------------------------
// HoverFilter – delegate for hover filtering (defined in the editor layer).
// ---------------------------------------------------------------------------
public delegate bool HoverFilter(ICADObject co);

// ---------------------------------------------------------------------------
// LengthMeasurementSystem – unit system for dimension display.
// ---------------------------------------------------------------------------
public enum LengthMeasurementSystem
{
    Millimetre,
    Centimetre,
    Metre,
    Inch,
}

// ---------------------------------------------------------------------------
// DetailSettings – settings container for the 3D model editor.
// ---------------------------------------------------------------------------
public class DetailSettings
{
    public LengthMeasurementSystem lengthMeasurement { get; set; } = LengthMeasurementSystem.Millimetre;
    public bool showConstraints   { get; set; } = true;
    public bool showDimensions    { get; set; } = true;
    public bool suppressSolver    { get; set; } = false;
    public bool detectContours    { get; set; } = true;
    public bool autoconstraining  { get; set; } = true;
    public bool drawingDimensions { get; set; } = true;
}

// ---------------------------------------------------------------------------
// Detail – top-level CAD document container (minimal stub).
// ---------------------------------------------------------------------------
public class Detail : Feature
{
    public string name { get; set; } = "";
    public Styles styles { get; } = new Styles();
    public List<Feature> features { get; } = new List<Feature>();
    public DetailSettings settings { get; } = new DetailSettings();

    public override GameObject gameObject { get; } = new GameObject("Detail");
    public override CADObject parentObject => null!;

    public override Id guid => Id.Null;
    public override ICADObject GetChild(Id guid) => null!;
    public Feature? GetFeature(Id guid) => features.Find(f => f.guid == guid);
}

// ---------------------------------------------------------------------------
// Feature – abstract CAD feature (minimal stub; full implementation in
// Features/*.cs which depends on the Csg geometry library).
// ---------------------------------------------------------------------------
public abstract class Feature : CADObject
{
    public Detail? detail_ { get; set; }
    public Detail detail
    {
        get => detail_!;
        set => detail_ = value;
    }
    public abstract GameObject gameObject { get; }
    public IdGenerator idGenerator { get; } = new IdGenerator();
}

// ---------------------------------------------------------------------------
// DraftStroke – rendering backend stub (real implementation uses Unity mesh
// rendering; this stub keeps Constraint.getPixelSize() compilable).
// ---------------------------------------------------------------------------
public class DraftStroke : MonoBehaviour
{
    public static double getGlobalPixelSize() => 0.01;
}

// ---------------------------------------------------------------------------
// UnityExt – utility extension methods for Unity math types.
// ---------------------------------------------------------------------------
public static class UnityExt
{
    public static Matrix4x4 Basis(Vector3 x, Vector3 y, Vector3 z, Vector3 p)
    {
        Matrix4x4 result = Matrix4x4.identity;
        result.SetColumn(0, new Vector4(x.x, x.y, x.z, 0));
        result.SetColumn(1, new Vector4(y.x, y.y, y.z, 0));
        result.SetColumn(2, new Vector4(z.x, z.y, z.z, 0));
        result.SetColumn(3, new Vector4(p.x, p.y, p.z, 1));
        return result;
    }

    public static Vector3 ToVector3(this string str)
    {
        var values = str.Split(' ');
        return new Vector3(values[0].ToFloat(), values[1].ToFloat(), values[2].ToFloat());
    }

    public static Quaternion ToQuaternion(this string str)
    {
        var values = str.Split(' ');
        return new Quaternion(values[0].ToFloat(), values[1].ToFloat(), values[2].ToFloat(), values[3].ToFloat());
    }

    public static string ToStr(this Vector3 v)    => v.x.ToStr() + " " + v.y.ToStr() + " " + v.z.ToStr();
    public static string ToStr(this Quaternion q) => q.x.ToStr() + " " + q.y.ToStr() + " " + q.z.ToStr() + " " + q.w.ToStr();

    public static void SetMatrix(this Transform tf, Matrix4x4 mtx)
    {
        tf.position = mtx.MultiplyPoint3x4(Vector3.zero);
        tf.rotation = Quaternion.LookRotation(mtx.GetColumn(2), mtx.GetColumn(1));
    }

    public static Vector3 WorldToGuiPoint(this Camera camera, Vector3 position)
    {
        var result = camera.WorldToScreenPoint(position);
        result.y = camera.pixelHeight - result.y;
        return result;
    }
}

// ---------------------------------------------------------------------------
// EntityConfig – global configuration singleton stub
// ---------------------------------------------------------------------------
public class EntityConfig
{
    private static EntityConfig? _instance;
    public static EntityConfig instance => _instance ??= new EntityConfig();

    public StrokeStyles styles { get; } = new StrokeStyles();
    public Material meshMaterial { get; set; } = new Material("mesh");
    public Material loopMaterial { get; set; } = new Material("loop");
}

// ---------------------------------------------------------------------------
// DetailEditor – editor state singleton stub
// The real implementation lives in the engine-specific layer; the Core
// library only needs a minimal surface to compile.
// ---------------------------------------------------------------------------
public class DetailEditor
{
    private static DetailEditor? _instance;
    public static DetailEditor instance => _instance ??= new DetailEditor();

    public object? hovered { get; set; }
    public List<object> selection { get; } = new List<object>();
    public HoverFilter? hoverFilter { get; set; }

    public bool IsSelected(object obj) => selection.Contains(obj);
}
