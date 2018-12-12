using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UndoRedo {
	DetailEditor editor;
	List<string> history = new List<string>();
	int pointer = 0;

	public int maxSize = 1000000;

	public UndoRedo(DetailEditor editor) {
		this.editor = editor;
	}

	public void Clear() {
		history.Clear();
		pointer = 0;
	}

	public void Push() {
		var count = history.Count - pointer;
		if(count > 0) {
			history.RemoveRange(pointer, count);
		}
		history.Add(editor.WriteXml());
		while(Size() > maxSize) history.RemoveAt(0);
		pointer = history.Count;
	}

	public void Pop() {
		if(history.Count > 0) {
			history.RemoveAt(history.Count - 1);
		}
		pointer = history.Count;
	}

	public bool CanUndo() {
		return pointer - 1 >= 0;
	}

	public void Undo() {
		if(!CanUndo()) return;
		if(pointer == history.Count) {
			Push();
			pointer--;
		}
		pointer--;
		Restore();
	}

	public bool CanRedo() {
		return pointer + 1 < history.Count;
	}

	public void Redo() {
		if(!CanRedo()) return;
		pointer++;
		Restore();
	}

	void Restore() {
		var active = editor.activeFeature.id;
		editor.ReadXml(history[pointer], readView: false, activateLast: false);
	}

	public int Count() {
		return history.Count;
	}

	public int Size() {
		return history.Sum(h => h.Length);
	}

}
