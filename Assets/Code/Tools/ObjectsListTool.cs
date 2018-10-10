using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectsListTool : Tool {

	class Input {
		public Type type;
		public IEntityType entityType;
		public ICADObject value;
		public Vector3 pos;
	}
	
	bool needToPlaceLabel = false;
	List<List<Input>> inputs = new List<List<Input>>();
	int listIndex = -1;
	int inputIndex = 0;
	bool labelPlaced = false;

	void AddInputs(List<Type> types) {
		List<Input> list = new List<Input>();
		foreach(var t in types) {
			var i = new Input();
			i.type = t;
			list.Add(i);
		}
		inputs.Add(list);
	}

	protected virtual void OnCreateObject(int index) {
		
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		/*if(needToPlaceLabel && labelPlaced == false) {
			labelPlaced = true;
			return;
		}*/
		if(sko == null) return;
		var type = sko.GetType();
		
		if(listIndex == -1) {
			for(int i = 0; i < inputs.Count; i++) {
				if(inputs[i][0].type != type) continue;
				listIndex = i;
				inputIndex++;
			}
		}
		if(inputIndex == -1) return;
		if(inputs[listIndex][inputIndex].type == type) {
			inputs[listIndex][inputIndex].value = sko;
			inputIndex++;
			if(inputIndex >= inputs[listIndex].Count) {
				OnCreateObject(listIndex);
				Clear();
			}
		}

	}

	void Clear() {
		foreach(var l in inputs) {
			foreach(var i in l) {
				i.value = null;
			}
		}
		listIndex = -1;
		inputIndex = 0;
	}

	protected override void OnDeactivate() {
		Clear();
	}

	protected override string OnGetDescription() {
		string result = "";
		if(listIndex == -1) {
			result = "click several entities one by one:";

			for(int i = 0; i < inputs.Count; i++) {
				if(i != 0) result += " or";
				for(int j = 0; j < inputs[i].Count; j++) {
					if(j != 0) result += " and";
					result += " " + inputs[i][j].type.Name;
				}
			}
		} else {
			result = "click next entity " + inputs[listIndex][inputIndex].type.Name;
		}

		return result;
	}

}
