**This documentation/commentary is very out-of-date. sorry!***

# gsSlicer

**In-Progress** Slicer for 3D printing, and other toolpath-type things, perhaps.

C#, MIT License (*but see notes below!*). Copyright 2017 ryan schmidt / gradientspace

questions? get in touch on twitter: [@rms80](http://www.twitter.com/rms80) or [@gradientspace](http://www.twitter.com/gradientspace), 
or email [rms@gradientspace.com](mailto:rms@gradientspace.com?subject=gsSlicer).

# What is this?

gsSlicer is an in-development open-source library for things like slicing 3D triangle meshes into planar polygons, filling those polygons with contour & raster fill paths, figuring out how much material to extrude along the paths, and then outputting GCode. The included **SliceViewer** project is also a GCode viewer/utility.

The goal with this project is to create a well-structured slicing engine that is designed from the ground up to be extensible. Although the initial focus will be on FDM/FFF-style printers, many of the parts of the system will be applicable to other processes like SLA, etc. Hopefully. Fingers crossed. At least, we'll definitely solve the meshes-to-slices problem for you.

# Current Status

**Under Active Development**. Generated GCode has been used for non-trivial prints, however the output has not been extensively tested. 

Shells, solid and sparse infill, roof and floors, have been implemented. Support volumes calculated but not yet filled. **ThreeAxisPrintGenerator** is top-level driver for FDM printing.

Experimental support for SLS contours & hatching is available in **GenericSLSPrintGenerator**. 

**Supported Printers**: At this time, only Makerbot Replicator 2 has been significantly tested. Also has been tested with Monoprice Select Mini (and hence should work with any generic RepRap) and Printrbot Metal Plus.


# Usage

This project is a source code library, not usable directly. GUI and command-line front ends are under development but very basic at this point, in the [gsSlicerApps](https://github.com/gradientspace/gsSlicerApps) project.

For an example of how to convert a mesh into GCode, see **GenerateGCodeForMeshes()** in *gsSlicerApps/sliceViewGTK/SliceViewerMain.cs*


# Dependencies

The slicing & path planning library **gsSlicer** depends on:

* [geometry3Sharp](https://github.com/gradientspace/geometry3Sharp) Boost license, git submodule
* [gsGCode](https://github.com/gradientspace/gsGCode) MIT license, git submodule
* [Clipper](http://www.angusj.com/delphi/clipper.php) by Angus Johnson, Boost license, embedded in /gsSlicer/thirdparty/clipper_library

No GPL/LGPL involved. All the code you would need to make an .exe that slices a mesh is available for unrestricted commercial use.


