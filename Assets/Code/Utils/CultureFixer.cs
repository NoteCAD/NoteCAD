using System.Globalization;
using System.Threading;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
 
#if UNITY_EDITOR
[InitializeOnLoad]
public static class FixCultureEditor {
    static FixCultureEditor() {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
    }
}
#endif
 
public static class FixCultureRuntime {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void FixCulture() {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
    }
}