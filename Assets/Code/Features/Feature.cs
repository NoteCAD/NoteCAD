using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class Feature {
	IEnumerator<Entity> entities { get { yield break; } }

	protected virtual void OnUpdate() { }

	public void Update() {
		OnUpdate();
	}

	public virtual void Write(XmlTextWriter xml) {
		//xml.WriteAttributeString("guid", guid.ToString());
		xml.WriteStartElement("feature");
		xml.WriteAttributeString("type", this.GetType().Name);
		OnWrite(xml);
		xml.WriteEndElement();
	}

	protected virtual void OnWrite(XmlTextWriter xml) {

	}

	public virtual void Read(XmlNode xml) {
		//guid = new Guid(xml.Attributes["guid"].Value);
		OnRead(xml);
	}

	protected virtual void OnRead(XmlNode xml)  {
	
	}

	protected virtual void OnClear() {

	}

	public void Clear() {
		OnClear();
	}
}
