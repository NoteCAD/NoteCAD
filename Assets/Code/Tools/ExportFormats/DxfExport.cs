using System.IO;
using ACadSharp.IO;

public class DxfExport : CadExportBase {
	public override byte[] GetResult() {
		using (var stream = new MemoryStream()) {
			using (var writer = new DxfWriter(stream, document, false)) {
				writer.Configuration.CloseStream = false;
				writer.Configuration.UpdateDimensionsInModel = true;
				writer.Write();
			}
			return stream.ToArray();
		}
	}
}
