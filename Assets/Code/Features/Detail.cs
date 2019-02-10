using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using UnityEngine;

public class Detail : Feature {

	public List<Feature> features = new List<Feature>();

	public override GameObject gameObject {
		get {
			return null;
		}
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
		Clear();
		var xml = new XmlDocument();
		xml.LoadXml(str);

		if(xml.DocumentElement.Attributes["id"] != null) {
			guid_ = idGenerator.Create(0);
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

		foreach(XmlNode node in xml.DocumentElement) {
			if(node.Name != "feature") continue;
			var type = node.Attributes["type"].Value;
			var item = Type.GetType(type).GetConstructor(new Type[0]).Invoke(new object[0]) as Feature;
			AddFeature(item);
			item.Read(node);
		}
	}

	public string WriteXml() {
		var text = new StringWriter();
		var xml = new XmlTextWriter(text);
		xml.Formatting = Formatting.Indented;
		xml.IndentChar = '\t';
		xml.Indentation = 1;
		xml.WriteStartDocument();
		xml.WriteStartElement("detail");
		xml.WriteAttributeString("id", guid.ToString());
		xml.WriteAttributeString("viewPos", Camera.main.transform.position.ToStr());
		xml.WriteAttributeString("viewRot", Camera.main.transform.rotation.ToStr());
		xml.WriteAttributeString("viewSize", Camera.main.orthographicSize.ToStr());
		xml.WriteAttributeString("activeFeature", DetailEditor.instance.activeFeature.id.ToString());

		foreach(var f in features) {
			f.Write(xml);
		}
		xml.WriteEndElement();
		return text.ToString();
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

	public ICADObject HoverUntil(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double objDist, Feature feature) {
		double min = -1.0;
		ICADObject result = null;
		foreach(var f in features) {
			if(!f.ShouldHoverWhenInactive() && !f.active) {
				continue;
			}
			double dist = -1.0;
			var hovered = f.Hover(mouse, camera, tf, ref dist);

			if(dist >= 0.0 && dist < Sketch.hoverRadius && (min < 0.0 || dist <= min)) {
				result = hovered;
				min = dist;
			}
			if(f == feature) break;
		}
		objDist = min;
		return result;
	}

	protected override ICADObject OnHover(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double objDist) {
		return HoverUntil(mouse, camera, tf, ref objDist, features.Last());
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
