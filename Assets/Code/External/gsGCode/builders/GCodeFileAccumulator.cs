using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gs
{

	public class GCodeFileAccumulator : IGCodeAccumulator
	{
		public GCodeFile File;

		public GCodeFileAccumulator(GCodeFile useFile = null) 
		{
			File = (useFile != null) ? useFile : new GCodeFile();
		}

		public virtual void AddLine(GCodeLine line) {
			File.AppendLine(line);
		}


	}


}