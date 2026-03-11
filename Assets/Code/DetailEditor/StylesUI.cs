using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

public class StylesUI : MonoBehaviour  {

	public Styles styles { get { return DetailEditor.instance.GetDetail().styles; } }
	public Dropdown dropdown;
	public static StylesUI instance;

	public Style selectedStyle {
		get {
			if(dropdown.options.Count == 0) return null;
			if(dropdown.value >= 0) {
				return styles.GetStyles().ElementAt(dropdown.value);
			}
			return null;
		}
	}

    // Start is called before the first frame update
    void Start() {
		instance = this;
    }

	public void UpdateStyles() {
		var selected = dropdown.value;
        dropdown.ClearOptions();
		dropdown.AddOptions(
			styles.GetStyles().Select(
				s => new Dropdown.OptionData(
					s.stroke.name, 
					DashAtlas.GeneratePreview(s.stroke.dashes, s.stroke.color, s.stroke.width)
				)
			).ToList()
		);
		dropdown.value = Math.Min(selected, dropdown.options.Count - 1);
	}

	public void SelectStyle(Style style) {
		dropdown.value = styles.GetStyles().ToList().IndexOf(style);
	}
	bool initialized = false;
    void Update() {
        if(!initialized) {
			UpdateStyles();
			initialized = true;
		}
    }
}
