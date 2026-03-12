# CMakeLists.txt – NativeOCC Unity plugin
#
# Builds the NativeOCC shared library that wraps OpenCASCADE boolean
# operations for use in NoteCAD native binary builds.
#
# Prerequisites
# -------------
# * OpenCASCADE 7.5 or later installed (or built from source).
#   Set the OCCT_INSTALL_DIR variable to the OCCT installation root if the
#   automatic search does not find it.
#
# Quick start
# -----------
#   mkdir build && cd build
#   cmake .. -DOCCT_INSTALL_DIR=/path/to/occt
#   cmake --build . --config Release
#
# The resulting library should be placed in the appropriate Unity Plugins
# subdirectory:
#   Windows 64-bit : Assets/Plugins/x86_64/NativeOCC.dll
#   Linux 64-bit   : Assets/Plugins/Linux64/libNativeOCC.so
#   macOS          : Assets/Plugins/macOS/NativeOCC.bundle  (or .dylib)

cmake_minimum_required(VERSION 3.15)
project(NativeOCC VERSION 1.0.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# -------------------------------------------------------------------------
# Locate OpenCASCADE
# -------------------------------------------------------------------------
# Users may provide -DOCCT_INSTALL_DIR=/path/to/occt on the cmake command line.
# Common installation paths are also searched automatically.
if(DEFINED OCCT_INSTALL_DIR)
    list(APPEND CMAKE_PREFIX_PATH "${OCCT_INSTALL_DIR}")
endif()

find_package(OpenCASCADE REQUIRED COMPONENTS
    FoundationClasses       # TKernel, TKMath
    ModelingData            # TKBRep, TKGeomBase, TKG3d
    ModelingAlgorithms      # TKGeomAlgo, TKTopAlgo, TKBool, TKBO, TKPrim, TKShHealing
    Visualization           # TKMesh (BRepMesh_IncrementalMesh)
)

# -------------------------------------------------------------------------
# Build the plugin
# -------------------------------------------------------------------------
add_library(NativeOCC SHARED OccWrapper.cpp)

target_include_directories(NativeOCC PRIVATE
    ${OpenCASCADE_INCLUDE_DIR}
)

# Core OCCT modules required by OccWrapper.cpp
set(OCC_LIBS
    TKBRep          # BRep topology
    TKMath          # gp_ geometry primitives
    TKG3d           # 3D geometry (gp_Pnt etc.)
    TKGeomBase      # Geometric algorithms base
    TKGeomAlgo      # Higher-level geometric algorithms
    TKTopAlgo       # Topological algorithms
    TKBool          # Boolean algorithm helpers
    TKBO            # BRepAlgoAPI_Fuse / Cut / Common
    TKPrim          # Primitive solids (BRepPrimAPI_*)
    TKMesh          # BRepMesh_IncrementalMesh
    TKShHealing     # BRepBuilderAPI_Sewing
)

target_link_libraries(NativeOCC PRIVATE ${OCC_LIBS})

# -------------------------------------------------------------------------
# Platform-specific output naming so Unity can find the plugin
# -------------------------------------------------------------------------
if(WIN32)
    # Unity looks for NativeOCC.dll in Assets/Plugins/x86_64/
    set_target_properties(NativeOCC PROPERTIES
        OUTPUT_NAME "NativeOCC"
        PREFIX ""
        SUFFIX ".dll"
    )
    # Export all symbols from the DLL (NATIVEOCC_EXPORTS defined via dllexport)
    target_compile_definitions(NativeOCC PRIVATE NATIVEOCC_EXPORTS)
elseif(APPLE)
    # Unity looks for NativeOCC.bundle in Assets/Plugins/macOS/
    set_target_properties(NativeOCC PROPERTIES
        OUTPUT_NAME "NativeOCC"
        PREFIX ""
        SUFFIX ".bundle"
        MACOSX_BUNDLE TRUE
    )
elseif(UNIX)
    # Unity looks for libNativeOCC.so in Assets/Plugins/Linux64/
    set_target_properties(NativeOCC PROPERTIES
        OUTPUT_NAME "NativeOCC"
        PREFIX "lib"
        SUFFIX ".so"
    )
endif()

# Hide all symbols by default; only the explicitly exported ones are visible.
if(NOT WIN32)
    target_compile_options(NativeOCC PRIVATE -fvisibility=hidden)
endif()

# -------------------------------------------------------------------------
# Install target – copies the library to the correct Unity Plugins directory
# -------------------------------------------------------------------------
if(WIN32)
    set(UNITY_PLUGINS_DIR "${CMAKE_SOURCE_DIR}/../../x86_64")
elseif(APPLE)
    set(UNITY_PLUGINS_DIR "${CMAKE_SOURCE_DIR}/../../macOS")
elseif(UNIX)
    set(UNITY_PLUGINS_DIR "${CMAKE_SOURCE_DIR}/../../Linux64")
endif()

install(TARGETS NativeOCC
    LIBRARY DESTINATION "${UNITY_PLUGINS_DIR}"
    RUNTIME DESTINATION "${UNITY_PLUGINS_DIR}"
)
