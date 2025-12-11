using System.Collections.Generic;
using System.Text;

public class Trans {
    
    static Dictionary<string, Dictionary<string, string>> translation = new();
    public static string currentLang = "EN";

    public static string late(string key, string def) {
        if (!translation.ContainsKey(key)) {
            translation[key] = new();
            translation[key][currentLang] = def; 
            translation[key]["EN"] = def;
        }
        if (!translation[key].ContainsKey(currentLang)) {
            translation[key][currentLang] = def;
        }
        return translation[key][currentLang];
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
            sb.Append(tr.Key);
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

