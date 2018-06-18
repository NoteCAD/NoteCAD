using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gs
{

	public interface IGCodeAccumulator
	{
		void AddLine(GCodeLine line);
	}


}