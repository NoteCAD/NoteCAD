using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using UnityEngine;
using System.Text;

public enum LengthMeasurementSystem {
	Millimetre,
	Centimetre,
	Metre,
	Inch
}

[Serializable]
public class DetailSettings {

	public enum MeshProviderFormat {
		JSON,
		XML
	}

	public enum DisplayPoints {
		All,
		CentersAndPoints,
		Points,
		None
	}

	public LengthMeasurementSystem lengthMeasurement = LengthMeasurementSystem.Millimetre;
	public bool showConstraints = true;
	public bool showDimensions = true;
	public DisplayPoints displayPoints = DisplayPoints.All;
	public bool autoconstraining = true;
	public bool drawingDimensions = true;
	public bool checkSketchErrors = true;
	public bool detectContours = true;
	public bool useMeshProvider = false;
	public bool suppressSolver = false;
	public string meshProvider = "http://localhost:8070/api?method=GenerateMesh";
	public MeshProviderFormat providerFormat = MeshProviderFormat.JSON;

	[RuntimeInspectorNamespace.RuntimeInspectorButton("Save Translation", false, RuntimeInspectorNamespace.ButtonVisibility.InitializedObjects)]
	void SaveTranslation() {
		NoteCADJS.SaveData(Trans.ToCSV(), "Translation", "csv");
	}
	
	public void Write(Writer xml) {
		xml.WriteBeginElement("settings");
		xml.WriteAttribute(nameof(lengthMeasurement), lengthMeasurement.ToString());
		xml.WriteAttribute(nameof(showConstraints), showConstraints);
		xml.WriteAttribute(nameof(showDimensions), showDimensions);
		xml.WriteAttribute(nameof(displayPoints), displayPoints);
		xml.WriteAttribute(nameof(autoconstraining), autoconstraining);
		xml.WriteAttribute(nameof(drawingDimensions), drawingDimensions);
		xml.WriteAttribute(nameof(checkSketchErrors), checkSketchErrors);
		xml.WriteAttribute(nameof(detectContours), detectContours);
		xml.WriteAttribute(nameof(suppressSolver), suppressSolver);
		xml.WriteEndElement();
	}

	public void Read(XmlNode xml) {
		foreach(XmlNode xmlChild in xml.ChildNodes) {
			if(xmlChild.Name != "settings") continue;
			xmlChild.GetAttribute(nameof(lengthMeasurement), ref lengthMeasurement);
			xmlChild.GetAttribute(nameof(showConstraints), ref showConstraints);
			xmlChild.GetAttribute(nameof(showDimensions), ref showDimensions);
			xmlChild.GetAttribute(nameof(displayPoints), ref displayPoints);
			xmlChild.GetAttribute(nameof(autoconstraining), ref autoconstraining);
			xmlChild.GetAttribute(nameof(drawingDimensions), ref drawingDimensions);
			xmlChild.GetAttribute(nameof(checkSketchErrors), ref checkSketchErrors);
			xmlChild.GetAttribute(nameof(detectContours), ref detectContours);
			xmlChild.GetAttribute(nameof(suppressSolver), ref suppressSolver);
		}
	}

}

public class Detail : Feature {

	public string name = "";
	public Styles styles = new Styles();

	public List<Feature> features = new List<Feature>();

	public DetailSettings settings { get; private set; }
	GameObject go;

	public override GameObject gameObject {
		get {
			return go;
		}
	}

	public Detail() {
		settings = new DetailSettings();
		go = new GameObject("Detail");
	}

	public override ICADObject GetChild(Id guid) {
		for(int i = 0; i < features.Count; i++) {
			if(features[i].guid == guid) {
				return features[i];
			}
		}
		return null;
	}

	protected override void OnUpdate() {
		foreach(var f in features) {
			f.Update();
		}
		if(features.Any(f => f.dirty)) {
			MarkDirty();
		}
	}

	public void AddFeature(Feature feature) {
		feature.detail = this;
		if (feature.gameObject != null) {
			feature.gameObject.transform.parent = gameObject.transform;
		}
		features.Add(feature);
	}

	protected override void OnClear() {
		foreach(var f in features) {
			f.Clear();
		}
		features.Clear();
	}

	protected override void OnUpdateDirty() {
		foreach(var f in features) {
			f.UpdateDirty();
		}
	}

	public void UpdateUntil(Feature until) {
		foreach(var f in features) {
			f.Update();
			if(f == until) break;
		}
		if(features.Any(f => f.dirty)) {
			MarkDirty();
		}
	}

	public void UpdateDirtyUntil(Feature until) {
		foreach(var f in features) {
			f.UpdateDirty();
			if(f == until) break;
		}
	}

	public void ReadXml(string str, bool readView, out IdPath active) {
		//#if XOR_ENCRYPTED
		if(!str.StartsWith("<?xml")) {
			str = Encrypton.Xor(str, 0xFADE2CAD);
		}
		//#endif

		Clear();
		var xml = new XmlDocument();
		xml.LoadXml(str);

		if(xml.DocumentElement.Attributes["id"] != null) {
			guid_ = idGenerator.Create(0);
		}

		name = "";
		if(xml.DocumentElement.Attributes["name"] != null) {
			name = xml.DocumentElement.Attributes["name"].Value;
		}

		if(readView) {
			if(xml.DocumentElement.Attributes["viewPos"] != null) {
				Camera.main.transform.position = xml.DocumentElement.Attributes["viewPos"].Value.ToVector3();
			}
			if(xml.DocumentElement.Attributes["viewRot"] != null) {
				Camera.main.transform.rotation = xml.DocumentElement.Attributes["viewRot"].Value.ToQuaternion();
			}
			if(xml.DocumentElement.Attributes["viewSize"] != null) {
				Camera.main.orthographicSize = xml.DocumentElement.Attributes["viewSize"].Value.ToFloat();
			}
		}

		if(xml.DocumentElement.Attributes["activeFeature"] != null) {
			active = IdPath.From(xml.DocumentElement.Attributes["activeFeature"].Value);
		} else {
			active = null;
		}
		settings.Read(xml.DocumentElement);
		foreach(XmlNode node in xml.DocumentElement) {
			if(node.Name == "styles") {
				styles.Read(node);
				continue;
			}
			if(node.Name != "feature") continue;
			var type = node.Attributes["type"].Value;
			var item = Type.GetType(type).GetConstructor(new Type[0]).Invoke(new object[0]) as Feature;
			AddFeature(item);
			item.Read(node);
		}
	}

	public string WriteXmlAsString(bool encrypt) {
		var text = new StringWriter();
		var xml = new XmlTextWriter(text);
		xml.Formatting = Formatting.Indented;
		xml.IndentChar = '\t';
		xml.Indentation = 1;
		WriteXml(xml);
		var result = text.ToString();
		if(encrypt) {
			result = Encrypton.Xor(result, 0xFADE2CAD);
		}
		return result;
	}

	public string WriteJsonAsString() {
		var json = new WriterJSON();
		WriteWrt(json);
		var result = json.ToString();
		return result;
	}


	public byte[] WriteXmlAsBinary() {
		var bin = new MemoryStream();
		var xml = XmlDictionaryWriter.CreateBinaryWriter(bin);
		WriteXml(xml);
		return bin.ToArray();
	}

	public void WriteXml(XmlWriter xmlW) {
		xmlW.WriteStartDocument();
		var xml = new WriterXml(xmlW);
		WriteWrt(xml);
	}

	public void WriteWrt(Writer wrt) {
		wrt.WriteBeginElement("detail");
		wrt.WriteAttribute("id", guid.ToString());
		wrt.WriteAttribute("name", name);
		wrt.WriteAttribute("viewPos", Camera.main.transform.position.ToStr());
		wrt.WriteAttribute("viewRot", Camera.main.transform.rotation.ToStr());
		wrt.WriteAttribute("viewSize", Camera.main.orthographicSize);
		wrt.WriteAttribute("activeFeature", DetailEditor.instance.activeFeature.id.ToString());
		settings.Write(wrt);
		styles.Write(wrt);
		wrt.WriteBeginFakeArray("features");
		foreach(var f in features) {
			f.Write(wrt);
		}
		wrt.WriteEndFakeArray();
		wrt.WriteEndElement();
	}

	public void MarqueeSelectUntil(Rect rect, bool wholeObject, Camera camera, Matrix4x4 tf, ref List<ICADObject> result, Feature feature) {
		foreach(var f in features) {
			if(!f.ShouldHoverWhenInactive() && !f.active) {
				continue;
			}
			f.MarqueeSelect(rect, wholeObject, camera, tf, ref result);
			if(f == feature) break;
		}
	}

	public ICADObject HoverUntil(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double objDist, Feature feature, HoverFilter filter) {
		double min = -1.0;
		ICADObject result = null;
		foreach(var f in features) {
			if(!f.ShouldHoverWhenInactive() && !f.active) {
				continue;
			}
			double dist = -1.0;
			var hovered = f.Hover(mouse, camera, tf, filter, ref dist);

			if(dist >= 0.0 && dist < Sketch.hoverRadius && (min < 0.0 || dist <= min)) {
				result = hovered;
				min = dist;
			}
			if(f == feature) break;
		}
		objDist = min;
		return result;
	}

	protected override ICADObject OnHover(Vector3 mouse, Camera camera, Matrix4x4 tf, HoverFilter filter, ref double objDist) {
		return HoverUntil(mouse, camera, tf, ref objDist, features.Last(), filter);
	}

	protected override void OnDraw(Matrix4x4 tf) {
		foreach(var f in features) {
			if(!f.visible) continue;
			if(!f.ShouldHoverWhenInactive() && !f.active) {
				continue;
			}
			f.Draw(tf);
		}
	}

	public Feature GetFeature(Id guid) {
		return features.Find(f => f.guid == guid);
	}

	public void MarkDirtyAfter(Feature feature) {
		bool mark = false;
		foreach(var f in features) {
			if(mark) {
				f.MarkDirty(true);
			}
			if(f == feature) {
				mark = true;
			}
		}
	}

}
