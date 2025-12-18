using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Trans {
    
    static Dictionary<string, Dictionary<string, string>> translation = new();
    public static string defaultLang = "EN";
    public static string currentLang = "EN";

    public static string late(string key, string def) {
        if(translation.TryGetValue(key, out var langs)) {
            if(langs.TryGetValue(currentLang, out var tr) && tr != "") {
                return tr;
            } else 
            if(langs.TryGetValue(defaultLang, out tr) && tr != "") {
                return tr;
            } else {
                langs[defaultLang] = def;
                return def;
            }
        }
        langs = new();
        langs[defaultLang] = def;
        translation[key] = langs;
        return def;
    }

    public static void late(MonoBehaviour ui, object obj) {
        if(obj == null) {
            return;
        }
		var texts = ui.GetComponentsInChildren<Text>();
		foreach(var t in texts) {
			if (t.gameObject.GetComponentInParent<InputField>() != null) {
				continue;
			}
			var translator = t.gameObject.GetComponent<Translator>();
			if (translator == null) {
				translator = t.gameObject.AddComponent<Translator>();
            }
			if (translator.key == "") {
			    var trKey = obj.GetType().Name + "@" + t.text.Replace(" ", "");
			    translator.key = trKey;
			    t.text = Trans.late(trKey, t.text);
			}
		}
    }

    public static void FromCSV(string csv) {
        translation = new Dictionary<string, Dictionary<string, string>>();
        var lines = csv.Split("\n");
        if(lines.Length == 0) {
            return;
        }
        var headerRow = lines[0].Replace("\r", "").Split('\t');
                        
        for(int l = 1; l < lines.Length; l++) {
            string line = lines[l].Replace("\r", "");
            var values = line.Split('\t');
                
            if (headerRow.Length == values.Length) {
                if (!translation.ContainsKey(values[0])) {
                    translation.Add(values[0], new Dictionary<string, string>());
                }
                var dict = translation[values[0]];
                for (int i = 1; i < values.Length; i++) {
                    if(values[i] == "") {
                        continue;
                    }
                    dict[headerRow[i]] = values[i];
                }
            }
        }
    }

    public static string ToCSV() {
        HashSet<string> langs = new();
        foreach(var v in translation.Values) {
            foreach(var lang in v.Keys) {
                langs.Add(lang);
            }
        }
        StringBuilder sb = new();
        sb.Append("Key");
        foreach(var lang in langs) {
            sb.Append("\t");
            sb.Append(lang);
        }
        sb.AppendLine();

        foreach(var tr in translation) {
            sb.Append(tr.Key.Replace("_", "@").Replace(" ", ""));
            foreach(var lang in langs)
            {
                sb.Append("\t");
                if (tr.Value.TryGetValue(lang, out var value)) {
                    sb.Append(value);
                }
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

}

