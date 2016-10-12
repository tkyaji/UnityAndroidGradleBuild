using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;


public static class GradleParser {

	public interface GradleElement { }

	public class GradleTextElement : GradleElement {
		public string Text;
		public GradleTextElement(string text) {
			this.Text = text;
		}
	}

	public class GradleBlockElement : GradleElement {
		public string Key;
		public GradleElement[] Elements;

		public GradleBlockElement(string key, GradleElement[] elements) {
			this.Key = key;
			this.Elements = elements;
		}

		public void AddElement(GradleElement addElement) {
			var elements = new List<GradleElement>(this.Elements);
			elements.Add(addElement);
			this.Elements = elements.ToArray();
		}

		public bool AddElement(GradleElement addElement, params string[] keys) {
			var targetBlockElement = findBlockElement(keys);
			if (targetBlockElement == null) {
				return false;
			}
			targetBlockElement.AddElement(addElement);
			return true;
		}

		public int RemoveElement(string regexString) {
			var elements = new List<GradleElement>(this.Elements);
			int removeCount = 0;
			for (int i = elements.Count - 1; i >= 0; i--) {
				var element = elements[i];
				if (element is GradleTextElement) {
					var textElement = element as GradleTextElement;
					var match = Regex.Match(textElement.Text, regexString);
					if (match.Success) {
						elements.RemoveAt(i);
						removeCount++;
					}
				}
			}
			this.Elements = elements.ToArray();
			return removeCount;
		}

		public int RemoveElement(string regexString, params string[] keys) {
			var targetBlockElement = findBlockElement(keys);
			if (targetBlockElement == null) {
				return 0;
			}
			return targetBlockElement.RemoveElement(regexString);
		}

		public GradleTextElement[] FindElements(string regexString) {
			var list = new List<GradleTextElement>();
			foreach (var element in this.Elements) {
				if (element is GradleTextElement) {
					var textElement = element as GradleTextElement;
					var match = Regex.Match(textElement.Text, regexString);
					if (match.Success) {
						list.Add(textElement);
					}
				}
			}
			return list.ToArray();
		}

		public GradleTextElement[] FindElements(string regexString, params string[] keys) {
			var targetBlockElement = findBlockElement(keys);
			return targetBlockElement.FindElements(regexString);
		}

		private GradleBlockElement findBlockElement(params string[] keys) {
			GradleBlockElement targetBlockElement = null;
			var targetElements = this.Elements;
			foreach (string key in keys) {
				bool found = false;
				foreach (var element in targetElements) {
					if (element is GradleBlockElement) {
						var blockElement = element as GradleBlockElement;
						if (blockElement.Key == key) {
							targetElements = blockElement.Elements;
							targetBlockElement = blockElement;
							found = true;
							break;
						}
					}
				}
				if (!found) {
					Debug.LogError("key not found : " + key);
					return null;
				}
			}

			return targetBlockElement;
		}
	}

	public class GradleRootElement : GradleBlockElement {
		public GradleRootElement(GradleElement[] elements) : base("root", elements) { }

		override public string ToString() {
			return formatBuildGradle(this);
		}

		private string formatBuildGradle(GradleBlockElement rootElement, int indent = 0) {
			StringBuilder strb = new StringBuilder();

			string indentText = new string(' ', indent * 4);

			foreach (var element in rootElement.Elements) {
				if (element is GradleTextElement) {
					var textElement = element as GradleTextElement;
					strb.AppendLine(indentText + textElement.Text);

				} else if (element is GradleBlockElement) {
					var blockElement = element as GradleBlockElement;
					strb.AppendLine(indentText + blockElement.Key + " {");
					string blockContent = formatBuildGradle(blockElement, indent + 1);
					strb.AppendLine(blockContent);
					strb.AppendLine(indentText + "}");
				}
			}

			return strb.ToString();
		}
	}


	public static GradleRootElement ParseBuildGradle(string buildGradleText) {

		var rootElementList = new List<GradleElement>();

		var stack = new Stack<KeyValuePair<string, List<GradleElement>>>();
		var matches = Regex.Matches(buildGradleText, "([a-zA-Z0-9_]+\\s*{|})");

		var currentElemenList = rootElementList;
		int prevIndex = 0;
		foreach (Match match in matches) {
			string subString = buildGradleText.Substring(prevIndex, match.Index - prevIndex);
			string[] lines = subString.Split(new char[] { ';', '\n' });

			var textElementList = new List<GradleElement>();
			foreach (string line in lines) {
				var l = line.Trim();
				if (l.Length > 0) {
					textElementList.Add(new GradleTextElement(l));
				}
			}
			currentElemenList.AddRange(textElementList);

			if (match.Value.EndsWith("{", System.StringComparison.Ordinal)) {
				string key = match.Value.Substring(0, match.Value.Length - 1).Trim();
				var pair = new KeyValuePair<string, List<GradleElement>>(key, currentElemenList);
				stack.Push(pair);
				prevIndex = match.Index + match.Value.Length;
				currentElemenList = new List<GradleElement>();

			} else if (match.Value == "}") {
				prevIndex = match.Index + 1;
				var pair = stack.Pop();
				var blockElement = new GradleBlockElement(pair.Key, currentElemenList.ToArray());
				currentElemenList = pair.Value;
				currentElemenList.Add(blockElement);
			}
		}

		return new GradleRootElement(rootElementList.ToArray());
	}

}
