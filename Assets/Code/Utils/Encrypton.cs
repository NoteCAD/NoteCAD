using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class Encrypton {
	public static string Xor(string text, uint key) {
		StringBuilder builder = new StringBuilder(text.Length);  
		for (int i = 0; i < text.Length; i++) {  
			builder.Append((char)(text[i] ^ key));
		}  
		return builder.ToString();  
	} 
}
