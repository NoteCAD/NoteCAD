using System;
using NoteCAD.Geometry;
using UnityEngine;

/// <summary>
/// Central access point for the active geometry kernel.
///
/// On WebGL builds the managed CSG library is always used, because native
/// plugins cannot be loaded inside the browser sandbox.
///
/// On native binary builds (Windows, macOS, Linux) the factory first tries to
/// load the NativeOCC plugin (OpenCASCADE-based); if that plugin is not present
/// or fails to initialise it falls back transparently to the managed CSG
/// implementation.
/// </summary>
public static class GeometryKernel {

	private static IBooleanOperations _booleanOps;

	/// <summary>
	/// The active <see cref="IBooleanOperations"/> implementation.
	/// Lazily initialised on first access.
	/// </summary>
	public static IBooleanOperations BooleanOps {
		get {
			if(_booleanOps == null) {
				_booleanOps = CreateBooleanOps();
			}
			return _booleanOps;
		}
	}

	/// <summary>
	/// Forces re-initialisation of the kernel on the next access.
	/// Useful when the native plugin is deployed at runtime.
	/// </summary>
	public static void Reset() => _booleanOps = null;

	// ---------------------------------------------------------------------- //

	private static IBooleanOperations CreateBooleanOps() {
#if UNITY_WEBGL
		// WebGL does not support native plugins – always use the managed fallback.
		Debug.Log("[GeometryKernel] WebGL build: using managed CSG boolean operations.");
		return new CsgBooleanOperations();
#else
		// On native platforms try to use OpenCASCADE for higher-quality results.
		// IsAvailable() already logs a warning when the plugin cannot be loaded.
		try {
			if(OccBooleanOperations.IsAvailable()) {
				Debug.Log("[GeometryKernel] NativeOCC plugin loaded. " +
				          "Using OpenCASCADE boolean operations.");
				return new OccBooleanOperations();
			}
		} catch(Exception ex) {
			Debug.LogWarning($"[GeometryKernel] Could not initialise NativeOCC: {ex.Message}");
		}
		Debug.Log("[GeometryKernel] NativeOCC not available. Using managed CSG boolean operations.");
		return new CsgBooleanOperations();
#endif
	}

}
