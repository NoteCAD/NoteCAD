using System;
using System.Collections;
using System.Text;
using System.IO;
using g3;

namespace gs 
{
	public class StandardGCodeWriter : BaseGCodeWriter 
	{
		int float_precision = 5;
		string float_format = "{0:0.#####}";
		public int FloatPrecision {
			get { return float_precision; }
			set { float_precision = value; 
				  float_format = "{0:0." + new String('#',float_precision) + "}"; 
			}
		}


		public override void WriteLine(GCodeLine line, StreamWriter outStream) 
		{
			if ( line.type == GCodeLine.LType.Comment ) {
				outStream.WriteLine(line.comment);
				return;
			} else if ( line.type == GCodeLine.LType.UnknownString ) {
				outStream.WriteLine(line.orig_string);
				return;
			} else if ( line.type == GCodeLine.LType.Blank ) {
				return;
			}

			StringBuilder b = new StringBuilder();
			if ( line.type == GCodeLine.LType.MCode ) {
				b.Append('M');
			} else if (line.type == GCodeLine.LType.GCode ) {
				b.Append('G');
			} else {
				throw new Exception("StandardGCodeWriter.WriteLine: unsupported line type");
			}

			b.Append(line.code);
			b.Append(' ');

			if ( line.parameters != null ) {
				foreach ( GCodeParam p in line.parameters ) {
					if ( p.type == GCodeParam.PType.Code ) {
						//
					} else if ( p.type == GCodeParam.PType.IntegerValue ) {
						b.Append(p.identifier);
						b.Append(p.intValue);
						b.Append(' ');
					} else if ( p.type == GCodeParam.PType.DoubleValue ) {
						b.Append(p.identifier);
						b.AppendFormat(float_format, p.doubleValue);
						b.Append(' ');
					} else if ( p.type == GCodeParam.PType.TextValue) {
						b.Append(p.identifier);
						b.Append(p.textValue);
						b.Append(' ');
					} else if ( p.type == GCodeParam.PType.NoValue) {
						b.Append(p.identifier);
						b.Append(' ');
					} else {
						throw new Exception("StandardGCodeWriter.WriteLine: unsupported parameter type");
					}
				}
			}


			if ( line.comment != null &&  line.comment.Length > 0 ) {
				if ( line.comment[0] != '(' && line.comment[0] != ';' )
					b.Append(';');
				b.Append(line.comment);
			}

			outStream.WriteLine(b.ToString());
		}

	}
}
