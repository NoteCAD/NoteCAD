# gsGCode

GCode parsing / manipulation / generation library. 

C#, MIT License. Copyright 2017 ryan schmidt / gradientspace

Dependencies: [geometry3Sharp](https://github.com/gradientspace/geometry3Sharp) (Boost license)

questions? get in touch on twitter: [@rms80](http://www.twitter.com/rms80) or [@gradientspace](http://www.twitter.com/gradientspace), 
or email [rms@gradientspace.com](mailto:rms@gradientspace.com?subject=gsGCode).

# What Is This?

gsGCode is a library for working with GCode. Not just generating GCode, but also reading and manipulating existing GCode files.

What? Edit a GCode file? that's crazy, right? CAD people used to think the same thing about STL mesh files too.
STL was something you wrote out to communicate with a CNC machine. You might "repair" it, but you didn't edit it.
But then a guy wrote [OpenSCAD](http://www.openscad.org/), and I wrote [Meshmixer](http://www.meshmixer.com), and now STL
files are something that thousands of people open and edit every day.

The purpose of this library is to make the same thing possible for GCode. GCode is just 3D lines, curves, and
control commands. There is no reason you can't open a GCode file, change one of the paths, and write it back out. 
You can easily do that with this library.

# Can It Write GCode Too?

Yes! I'm developing gsGCode to support the gradientspace Slicer, which is not released yet. But the low-level GCode generation 
infrastructure is all here. You can either use **GCodeBuilder** to construct a **GCodeFile** line-by-line, or an Assembler 
(currently only **MakerbotAssembler**) which provides a higher-level turtle-graphics-like API. 

# How?

See [the wiki](https://github.com/gradientspace/gsGCode/wiki) for what minimal documentation exists right now.
Some sample code will come in time. 
