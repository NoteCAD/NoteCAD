/*
 * OccWrapper.h
 *
 * C API for the NativeOCC Unity plugin.
 *
 * This header describes the functions exported by OccWrapper.cpp and imported
 * via P/Invoke from OccBooleanOperations.cs.  All functions use the cdecl
 * calling convention and have plain-C linkage so that name-mangling is avoided.
 *
 * Build with CMakeLists.txt (see same directory) after installing OpenCASCADE.
 */

#pragma once

#ifdef _WIN32
#  ifdef NATIVEOCC_EXPORTS
#    define NATIVEOCC_API __declspec(dllexport)
#  else
#    define NATIVEOCC_API __declspec(dllimport)
#  endif
#else
#  define NATIVEOCC_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* -------------------------------------------------------------------------
 * Boolean operation codes (must match the BoolOp enum in OccBooleanOperations.cs)
 * ---------------------------------------------------------------------- */
#define OCC_OP_UNION        0
#define OCC_OP_DIFFERENCE   1
#define OCC_OP_INTERSECTION 2

/* -------------------------------------------------------------------------
 * OCC_BooleanOp
 *
 * Performs a Boolean operation on two triangle meshes using OpenCASCADE.
 *
 * Input meshes are described by flat arrays:
 *   vertices  – x0,y0,z0, x1,y1,z1, … (3 floats per vertex)
 *   triangles – i0,i1,i2, i3,i4,i5, … (3 ints per triangle, 0-based indices)
 *
 * The result mesh is allocated by the plugin.  The caller must release it
 * with two separate calls to OCC_FreeMemory (one for outVertices, one for
 * outTriangles).
 *
 * Returns 0 on success, a non-zero error code on failure.
 * ---------------------------------------------------------------------- */
NATIVEOCC_API int OCC_BooleanOp(
    int         operation,
    const float* aVertices,   int aVertexCount,
    const int*   aTriangles,  int aTriangleCount,
    const float* bVertices,   int bVertexCount,
    const int*   bTriangles,  int bTriangleCount,
    float**      outVertices, int* outVertexCount,
    int**        outTriangles, int* outTriangleCount
);

/* -------------------------------------------------------------------------
 * OCC_FreeMemory
 *
 * Frees a buffer previously allocated by OCC_BooleanOp.
 * ---------------------------------------------------------------------- */
NATIVEOCC_API void OCC_FreeMemory(void* ptr);

/* -------------------------------------------------------------------------
 * OCC_GetVersion
 *
 * Returns a null-terminated string with the linked OCCT version (e.g. "7.7.0").
 * The string is owned by the plugin and must NOT be freed by the caller.
 * ---------------------------------------------------------------------- */
NATIVEOCC_API const char* OCC_GetVersion(void);

#ifdef __cplusplus
} /* extern "C" */
#endif
