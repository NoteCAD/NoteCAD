using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sketch : MonoBehaviour {
	List<Entity> entities = new List<Entity>();
	List<Constraint> constraints = new List<Constraint>();
	public static Sketch instance;
	public Text resultText;
	bool sysDirty;
	EquationSystem sys = new EquationSystem();
	Exp dragX;
	Exp dragY;

	Entity hovered_;
	Color oldColor;
	public Entity hovered {
		get {
			return hovered_;
		}
		set {
			if(hovered_ == value) return;
			if(hovered_ != null) {
				var r = hovered_.gameObject.GetComponent<Renderer>();
				r.material.color = oldColor;
			}
			hovered_ = value;
			if(hovered_ != null) {
				var r = hovered_.gameObject.GetComponent<Renderer>();
				oldColor = r.material.color;
				r.material.color = Color.yellow;
			}
		}
	}

	private void Start() {
		instance = this;
		/*
		PointEntity[] pr = null;
		for(int i = 0; i < 10; i++) {
			var nr = CreateRectangle(new Vector3(i * 5, 0, 0));
			if(pr != null) {
				sys.AddEquation(new Exp(pr[1].x) - nr[0].x);
				sys.AddEquation(new Exp(pr[1].y) - nr[0].y);
			}
			pr = nr;
		}
		sys.AddEquation(new Exp(pr[3].x));
		sys.AddEquation(new Exp(pr[3].y));
		*/
	}

	public void SetDrag(Exp dragX, Exp dragY) {
		if(this.dragX != dragX) {
			if(this.dragX != null) sys.RemoveEquation(this.dragX);
			this.dragX = dragX;
			if(dragX != null) sys.AddEquation(dragX);
		}
		if(this.dragY != dragY) {
			if(this.dragY != null) sys.RemoveEquation(this.dragY);
			this.dragY = dragY;
			if(dragY != null) sys.AddEquation(dragY);
		}
	} 

	PointEntity[] CreateRectangle(Vector3 pos) {
		var p = new PointEntity[4];
		for(int i = 0; i < p.Length; i++) {
			p[i] = CreatePoint();
			p[i].x.name = "x" + i.ToString();
			p[i].y.name = "y" + i.ToString();
		}

		p[0].SetPosition(new Vector3(0, 0, 0));
		p[1].SetPosition(new Vector3(5, 0, 0));
		p[2].SetPosition(new Vector3(5, 10, 0));
		p[3].SetPosition(new Vector3(0, 10, 0));
		for(int i = 0; i < p.Length; i++) {
			p[i].SetPosition(p[i].GetPosition() + pos);
		}
		
		sys.AddEquation((p[0].GetPositionExp() - p[1].GetPositionExp()).Magnitude() - 5.0);
		sys.AddEquation((p[1].GetPositionExp() - p[2].GetPositionExp()).Magnitude() - 10.0);
		sys.AddEquation((p[2].GetPositionExp() - p[3].GetPositionExp()).Magnitude() - 5.0);
		sys.AddEquation(DirCos(p[0].exp - p[1].exp, p[2].exp - p[1].exp));
		sys.AddEquation(DirCos(p[1].exp - p[2].exp, p[3].exp - p[2].exp));
		return p;
	}


	Exp DirCos(ExpVector a, ExpVector b) {
		return ExpVector.Dot(a, b) / (a.Magnitude() * b.Magnitude());
	}

	float DirCos(Vector3 a, Vector3 b) {
		return Vector3.Dot(a, b) / (a.magnitude * b.magnitude);
	}

	public PointEntity CreatePoint() {
		return new PointEntity(this);
	}

	public void CreateLine() {
		new LineEntity(this);
	}

	public void AddEntity(Entity e) {
		if(entities.Contains(e)) return;
		entities.Add(e);
		sysDirty = true;
	}

	public void AddConstraint(Constraint c) {
		if(constraints.Contains(c)) return;
		constraints.Add(c);
		sysDirty = true;
	}


	void UpdateSystem() {
		if(!sysDirty) return;
		sys.Clear();
		foreach(var e in entities) {
			sys.AddParameters(e.parameters);
			sys.AddEquations(e.equations);
		}
		foreach(var c in constraints) {
			sys.AddParameters(c.parameters);
			sys.AddEquations(c.equations);
		}
		sysDirty = false;
	}

	private void Update() {
		UpdateSystem();
		var result = sys.Solve();
		resultText.text = result.ToString();
	}

	private void LateUpdate() {
		foreach(var c in constraints) {
			c.Draw();
		}
		MarkUnchanged();
	}

	public void MarkUnchanged() {
		foreach(var e in entities) {
			foreach(var p in e.parameters) {
				p.changed = false;
			}
		}
		foreach(var c in constraints) {
			foreach(var p in c.parameters) {
				p.changed = false;
			}
		}
	}
}
