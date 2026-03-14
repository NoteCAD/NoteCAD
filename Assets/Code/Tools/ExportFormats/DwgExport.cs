using System.IO;
using ACadSharp.IO;

public class DwgExport : CadExportBase {
	public override byte[] GetResult() {
		using (var stream = new MemoryStream()) {
			using (var writer = new DwgWriter(stream, document)) {
				writer.Configuration.CloseStream = false;
				writer.Configuration.UpdateDimensionsInModel = true;
				writer.Write();
			}
			return stream.ToArray();
		}
	}
}
