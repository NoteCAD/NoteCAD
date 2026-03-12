/*
 * OccWrapper.cpp
 *
 * Native OpenCASCADE (OCCT) wrapper for the NoteCAD geometry kernel.
 *
 * This file implements the C API declared in OccWrapper.h.  It is compiled
 * into the NativeOCC shared library that is loaded at runtime by Unity on
 * Windows, macOS and Linux binary builds.  WebGL builds never load this
 * library; they use the managed CsgBooleanOperations fallback instead.
 *
 * Algorithm overview
 * ------------------
 * 1. A triangle mesh (vertex + index arrays) is converted into an OCCT
 *    BRep solid by:
 *      a. Creating one TopoDS_Face per triangle using a Poly_Triangulation.
 *      b. Sewing all faces into a shell/solid with BRepBuilderAPI_Sewing.
 *      c. Promoting the shell to a closed solid with BRepBuilderAPI_MakeSolid.
 * 2. The two input solids are combined using one of:
 *      BRepAlgoAPI_Fuse       (Union)
 *      BRepAlgoAPI_Cut        (Difference)
 *      BRepAlgoAPI_Common     (Intersection)
 * 3. The result shape is tessellated with BRepMesh_IncrementalMesh and the
 *    triangulation is extracted face-by-face and written into the output arrays.
 *
 * Build requirements
 * ------------------
 * OpenCASCADE 7.5 or later.  The following OCCT modules are required:
 *   TKBRep  TKMath  TKG3d  TKGeomBase  TKGeomAlgo
 *   TKTopAlgo  TKBool  TKBO  TKPrim  TKMesh  TKShHealing
 *
 * See CMakeLists.txt in this directory for the full build configuration.
 */

#include "OccWrapper.h"

#include <cstdlib>
#include <cstring>
#include <vector>
#include <stdexcept>

/* OCCT headers */
#include <Standard_Version.hxx>

/* BRep / topology */
#include <BRep_Builder.hxx>
#include <BRepBuilderAPI_MakeSolid.hxx>
#include <BRepBuilderAPI_Sewing.hxx>
#include <TopoDS.hxx>
#include <TopoDS_Compound.hxx>
#include <TopoDS_Face.hxx>
#include <TopoDS_Shape.hxx>
#include <TopoDS_Shell.hxx>
#include <TopoDS_Solid.hxx>
#include <TopExp_Explorer.hxx>

/* Polygon triangulation */
#include <BRep_Tool.hxx>
#include <Poly_Triangulation.hxx>

/* Meshing */
#include <BRepMesh_IncrementalMesh.hxx>

/* Boolean operations */
#include <BRepAlgoAPI_Fuse.hxx>
#include <BRepAlgoAPI_Cut.hxx>
#include <BRepAlgoAPI_Common.hxx>

/* -------------------------------------------------------------------------
 * Internal helpers
 * ---------------------------------------------------------------------- */

/* Build an OCCT BRep solid from a flat vertex/triangle mesh. */
static TopoDS_Shape MeshToShape(
    const float* vertices, int vertexCount,
    const int*   triangles, int triangleCount)
{
    BRep_Builder builder;

    /* Build a Poly_Triangulation-backed compound: one face per triangle. */
    TopoDS_Compound compound;
    builder.MakeCompound(compound);

    for(int t = 0; t < triangleCount; ++t) {
        int i0 = triangles[t * 3 + 0];
        int i1 = triangles[t * 3 + 1];
        int i2 = triangles[t * 3 + 2];

        gp_Pnt p0(vertices[i0*3], vertices[i0*3+1], vertices[i0*3+2]);
        gp_Pnt p1(vertices[i1*3], vertices[i1*3+1], vertices[i1*3+2]);
        gp_Pnt p2(vertices[i2*3], vertices[i2*3+1], vertices[i2*3+2]);

        /* Create a single-triangle Poly_Triangulation. */
        Handle(Poly_Triangulation) poly =
            new Poly_Triangulation(3, 1, Standard_False);

        poly->SetNode(1, p0);
        poly->SetNode(2, p1);
        poly->SetNode(3, p2);
        poly->SetTriangle(1, Poly_Triangle(1, 2, 3));

        TopoDS_Face face;
        builder.MakeFace(face, poly);
        builder.Add(compound, face);
    }

    /* Sew the individual faces into a manifold solid. */
    BRepBuilderAPI_Sewing sewing(1e-6);
    sewing.Add(compound);
    sewing.Perform();
    TopoDS_Shape sewn = sewing.SewedShape();

    /* Promote the outer shell to a solid if possible. */
    if(sewn.ShapeType() == TopAbs_SHELL) {
        BRepBuilderAPI_MakeSolid mkSolid(TopoDS::Shell(sewn));
        if(mkSolid.IsDone()) {
            return mkSolid.Solid();
        }
    }

    return sewn;
}

/* Extract the tessellated mesh from an OCCT shape into flat arrays.
 * Caller owns the returned vectors. */
static void ShapeToMesh(
    const TopoDS_Shape& shape,
    std::vector<float>& outVertices,
    std::vector<int>&   outTriangles)
{
    /* Ensure a triangulation exists on every face. */
    BRepMesh_IncrementalMesh mesher(shape, 0.1 /* linear deflection */);
    mesher.Perform();

    for(TopExp_Explorer ex(shape, TopAbs_FACE); ex.More(); ex.Next()) {
        TopoDS_Face face = TopoDS::Face(ex.Current());
        TopLoc_Location loc;
        Handle(Poly_Triangulation) poly = BRep_Tool::Triangulation(face, loc);
        if(poly.IsNull() || poly->NbTriangles() == 0) continue;

        int baseIdx = static_cast<int>(outVertices.size()) / 3;

        /* Vertices */
        for(int n = 1; n <= poly->NbNodes(); ++n) {
            gp_Pnt p = poly->Node(n);
            if(!loc.IsIdentity()) {
                p.Transform(loc);
            }
            outVertices.push_back(static_cast<float>(p.X()));
            outVertices.push_back(static_cast<float>(p.Y()));
            outVertices.push_back(static_cast<float>(p.Z()));
        }

        /* Triangles (OCCT node indices are 1-based) */
        bool reversed = (face.Orientation() == TopAbs_REVERSED);
        for(int t = 1; t <= poly->NbTriangles(); ++t) {
            Poly_Triangle tri = poly->Triangle(t);
            int n1, n2, n3;
            tri.Get(n1, n2, n3);
            if(reversed) std::swap(n2, n3);
            outTriangles.push_back(baseIdx + n1 - 1);
            outTriangles.push_back(baseIdx + n2 - 1);
            outTriangles.push_back(baseIdx + n3 - 1);
        }
    }
}

/* -------------------------------------------------------------------------
 * Exported C API
 * ---------------------------------------------------------------------- */

extern "C" {

NATIVEOCC_API int OCC_BooleanOp(
    int          operation,
    const float* aVertices,    int aVertexCount,
    const int*   aTriangles,   int aTriangleCount,
    const float* bVertices,    int bVertexCount,
    const int*   bTriangles,   int bTriangleCount,
    float**      outVertices,  int* outVertexCount,
    int**        outTriangles, int* outTriangleCount)
{
    if(!outVertices || !outVertexCount || !outTriangles || !outTriangleCount)
        return -1;

    *outVertices     = nullptr;
    *outVertexCount  = 0;
    *outTriangles    = nullptr;
    *outTriangleCount = 0;

    try {
        TopoDS_Shape shapeA = MeshToShape(aVertices, aVertexCount, aTriangles, aTriangleCount);
        TopoDS_Shape shapeB = MeshToShape(bVertices, bVertexCount, bTriangles, bTriangleCount);

        TopoDS_Shape result;
        switch(operation) {
            case OCC_OP_UNION: {
                BRepAlgoAPI_Fuse fuse(shapeA, shapeB);
                if(!fuse.IsDone()) return 1;
                result = fuse.Shape();
                break;
            }
            case OCC_OP_DIFFERENCE: {
                BRepAlgoAPI_Cut cut(shapeA, shapeB);
                if(!cut.IsDone()) return 2;
                result = cut.Shape();
                break;
            }
            case OCC_OP_INTERSECTION: {
                BRepAlgoAPI_Common common(shapeA, shapeB);
                if(!common.IsDone()) return 3;
                result = common.Shape();
                break;
            }
            default:
                return -2; /* unknown operation */
        }

        std::vector<float> verts;
        std::vector<int>   tris;
        ShapeToMesh(result, verts, tris);

        /* Allocate output buffers using malloc so that OCC_FreeMemory can
         * safely release them with free() regardless of element type. */
        float* vbuf = static_cast<float*>(std::malloc(verts.size() * sizeof(float)));
        int*   tbuf = static_cast<int*>  (std::malloc(tris.size()  * sizeof(int)));
        if(!vbuf || !tbuf) {
            std::free(vbuf);
            std::free(tbuf);
            return -5;
        }
        std::memcpy(vbuf, verts.data(), verts.size() * sizeof(float));
        std::memcpy(tbuf, tris.data(),  tris.size()  * sizeof(int));

        *outVertices      = vbuf;
        *outVertexCount   = static_cast<int>(verts.size()) / 3;
        *outTriangles     = tbuf;
        *outTriangleCount = static_cast<int>(tris.size())  / 3;

        return 0;
    }
    catch(const std::exception& ex) {
        (void)ex; /* Error details could be forwarded via a separate API */
        return -3;
    }
    catch(...) {
        return -4;
    }
}

NATIVEOCC_API void OCC_FreeMemory(void* ptr) {
    std::free(ptr);
}

NATIVEOCC_API const char* OCC_GetVersion(void) {
    return OCC_VERSION_COMPLETE; /* e.g. "7.7.0" – macro from Standard_Version.hxx */
}

} /* extern "C" */
