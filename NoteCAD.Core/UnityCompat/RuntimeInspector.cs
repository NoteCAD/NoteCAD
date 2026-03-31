// Stub for RuntimeInspectorNamespace used in Styles.cs and other files.
// In the open-source build these attributes are no-ops; a real inspector
// integration should replace this file with actual bindings.

namespace RuntimeInspectorNamespace
{
    public enum ButtonVisibility
    {
        Undefined = 0,
        InitializedObjects = 1,
        UninitializedObjects = 2,
        Everything = 3,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public sealed class RuntimeInspectorButtonAttribute : System.Attribute
    {
        public string Label        { get; }
        public bool   ShowOnTop    { get; }
        public ButtonVisibility Visibility { get; }

        public RuntimeInspectorButtonAttribute(string label, bool showOnTop, ButtonVisibility visibility)
        {
            Label      = label;
            ShowOnTop  = showOnTop;
            Visibility = visibility;
        }
    }
}
