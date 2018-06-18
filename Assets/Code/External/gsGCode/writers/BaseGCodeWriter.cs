using System;
using System.IO;
using g3;
using System.Collections;
using System.Collections.Generic;

namespace gs 
{
	public abstract class BaseGCodeWriter 
	{

		public virtual void WriteFile(GCodeFile file, StreamWriter outStream) 
		{
			foreach ( var line in file.AllLines() )
				WriteLine(line, outStream);
		}

		public virtual IEnumerable<Progress> WriteFileEnumerator(GCodeFile file, StreamWriter outStream) 
		{
			int i = 0;
			foreach ( var line in file.AllLines() ) {
				WriteLine(line, outStream);
				if(i++ % 1000 == 0) yield return new Progress("gcode", i, file.AllLinesCount());
			}
		}



		public abstract void WriteLine(GCodeLine line, StreamWriter outStream);

	}
}
