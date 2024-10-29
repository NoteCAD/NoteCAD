using System.Collections.Generic;
using System.IO;
using System.Text;

public class Translate {
    
    static Dictionary<string, Dictionary<string, string>> translation;
    static string currentLang = "RU";

    static string Tr(string key) {
        if (!translation.ContainsKey(key)) {
            translation[key][currentLang] = "#" + key;
        }
        return translation[key][currentLang];
    }
    static string Tr(string key, string def) {
        if (!translation.ContainsKey(key)) {
            translation[key]["EN"] = key; 
        }
        return translation[key][currentLang];
    }

    public void LoadCsvFile(string filePath) {
        translation = new Dictionary<string, Dictionary<string, string>>();
        
        using (var reader = new StreamReader(filePath)) {
            var headerRow = reader.ReadLine().Split('\t');
                        
            while (!reader.EndOfStream) {
                string line = reader.ReadLine();
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
    }

    public void SaveCsvFile(string filePath) {

        var builder = new StringBuilder();

    }

}

