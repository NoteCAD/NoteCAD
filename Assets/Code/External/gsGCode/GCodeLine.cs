using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace gs
{



	public struct GCodeParam
	{
		public enum PType
		{
			Code,
			DoubleValue,
			IntegerValue,
			TextValue,
			NoValue,
			Unknown
		}

		public PType type;
		public string identifier;

		public double doubleValue;      
		public int intValue {
			get { return (int)doubleValue; }    // we can store [-2^54, 2^54] precisely in a double
			set { doubleValue = value; }
		}
		public string textValue;
	}




	// ugh..class...dangerous!!
	public class GCodeLine
	{
		public enum LType
		{
			GCode,
			MCode, 
			UnknownCode,

			Comment,
			UnknownString,
			Blank,

			If,
			EndIf,
			Else,
			UnknownControl
		}



		public int lineNumber;

		public LType type;
		public string orig_string;

		public int N;       // N number of line
		public int code;    // G or M code
		public GCodeParam[] parameters;      // arguments/parameters

		public string comment;

		public GCodeLine(int num, LType type)
		{
			lineNumber = num;
			this.type = type;

			orig_string = null;
			N = code = -1;
			parameters = null;
			comment = null;
		}

		public GCodeLine(int lineNum, LType type, string comment) {
			lineNumber = lineNum;
			this.type = type;			
			if ( type == LType.UnknownString ) {
				this.orig_string = comment;
			} else {
				this.comment = comment;
			}
		}


		public virtual GCodeLine Clone() {
			GCodeLine clone = new GCodeLine(this.lineNumber, this.type);
			clone.orig_string = this.orig_string;
			clone.N = this.N;
			clone.code = this.code;
			if ( this.parameters != null ) {
				clone.parameters = new GCodeParam[this.parameters.Length];
				for (int i = 0; i < this.parameters.Length; ++i )
					clone.parameters[i] = this.parameters[i];
			}
			clone.comment = this.comment;
			return clone;
		}
			
	}


}