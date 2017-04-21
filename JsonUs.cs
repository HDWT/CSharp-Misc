using System.Collections.Generic;
using System.Text;

public static class JsonUs
{
	private static readonly StringBuilder s_stringBuilder = new StringBuilder(100);

	public static bool TryGet(string json, string key, out int value)
	{
		value = 0;

		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(json))
			return false;

		string stringValue = null;
		if (!TryGet(key, json, out stringValue))
			return false;

		if (!int.TryParse(stringValue, out value))
			return false;

		return true;
	}

	public static bool TryGet(string json, string key, out float value)
	{
		value = 0;

		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(json))
			return false;

		string stringValue = null;
		if (!TryGet(key, json, out stringValue))
			return false;

		if (!float.TryParse(stringValue, out value))
			return false;

		return true;
	}

	public static bool TryGet(string json, string key, out string value)
	{
		value = null;

		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(json))
			return false;

		int keyStart = 0;
		int keyLength = 0;
		int index = 0;

		if (!SetNextKey(key, ref keyStart, ref keyLength))
			return false;

		lock (s_stringBuilder)
			value = GetValue(json, s_stringBuilder, key, keyStart, keyLength, ref index);

		return (value != null);
	}

	public static bool TryGet(string json, string key, out List<int> value)
	{
		value = null;

		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(json))
			return false;

		List<string> stringValues = null;
		if (!TryGet(key, json, out stringValues))
			return false;

		value = new List<int>(stringValues.Count);

		bool succeeded = true;

		for (int i = 0; i < stringValues.Count; ++i)
		{
			int intValue = 0;

			if (!int.TryParse(stringValues[i], out intValue))
			{
				intValue = 0;
				succeeded = false;
			}

			value.Add(intValue);
		}

		return succeeded;
	}

	public static bool TryGet(string json, string key, out List<float> value)
	{
		value = null;

		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(json))
			return false;

		List<string> stringValues = null;
		if (!TryGet(key, json, out stringValues))
			return false;

		value = new List<float>(stringValues.Count);

		bool succeeded = true;

		for (int i = 0; i < stringValues.Count; ++i)
		{
			float floatValue = 0;

			if (!float.TryParse(stringValues[i], out floatValue))
			{
				floatValue = 0;
				succeeded = false;
			}

			value.Add(floatValue);
		}

		return succeeded;
	}

	public static bool TryGet(string json, string key, out List<string> value)
	{
		value = null;

		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(json))
			return false;

		int keyStart = 0;
		int keyLength = 0;
		int index = 0;

		if (!SetNextKey(key, ref keyStart, ref keyLength))
			return false;

		string stringValue = null;

		lock (s_stringBuilder)
			stringValue = GetValue(json, s_stringBuilder, key, keyStart, keyLength, ref index);

		if (stringValue == null)
			return false;

		//
		index = 0;

		if (SkipWhiteSpaces(stringValue, ref index) == stringValue.Length)
			return false;

		if (stringValue[index] == '[')
		{
			value = GetArray(stringValue, s_stringBuilder, ref index);
		}
		else
		{
			value = new List<string>() { stringValue };
		}

		return (value != null);
	}

	// ---------------------------------------------------------------------------

	private static string GetValue(string json, StringBuilder stringBuilder, string key, int keyStart, int keyLength, ref int index)
	{
		stringBuilder.Length = 0;

		if (SkipWhiteSpaces(json, ref index) == json.Length)
			return null;

		if (json[index++] != '{')
			return null;

		if (SkipWhiteSpaces(json, ref index) == json.Length)
			return null;

		if (json[index] == '}')
			return null;

		do
		{
			int isKeyValue = IsKey(json, key, keyStart, keyLength, ref index);

			if (isKeyValue == -1)
				return null;

			if (SkipWhiteSpaces(json, ref index) == json.Length)
				return null;

			if (json[index++] != ':')
				return null;

			if (SkipWhiteSpaces(json, ref index) == json.Length)
				return null;

			if (isKeyValue == 1)
			{
				if (keyStart + keyLength == key.Length)
				{
					if (!WriteValue(json, stringBuilder, ref index))
						return null;

					return stringBuilder.ToString();
				}
				else
				{
					keyLength += 1; // Skip '.' and get next key

					if (!SetNextKey(key, ref keyStart, ref keyLength))
						return null;

					return GetValue(json, stringBuilder, key, keyStart, keyLength, ref index);
				}
			}
			else if (isKeyValue == 0)
			{
				if (!WriteValue(json, null, ref index))
					return null;
			}
			else
			{
				throw new System.ArgumentException();
			}

			if (SkipWhiteSpaces(json, ref index) == json.Length)
				return null;

			if (json[index++] != ',')
				return null;

			if (SkipWhiteSpaces(json, ref index) == json.Length)
				return null;

		} while (true);
	}

	private static List<string> GetArray(string json, StringBuilder stringBuilder, ref int index)
	{
		stringBuilder.Length = 0;

		if (SkipWhiteSpaces(json, ref index) == json.Length)
			return null;

		if (json[index++] != '[')
			return null;

		if (SkipWhiteSpaces(json, ref index) == json.Length)
			return null;

		if (json[index] == ']')
			return new List<string>();

		List<string> values = new List<string>();

		do
		{
			if (!WriteValue(json, stringBuilder, ref index))
				return null;

			string arrayValue = stringBuilder.ToString();

			values.Add(arrayValue);

			if (SkipWhiteSpaces(json, ref index) == json.Length)
				return values;

			if (json[index] == ']')
				return values;

			if (json[index++] != ',')
				return null;

			if (SkipWhiteSpaces(json, ref index) == json.Length)
				return values;

		} while (true);
	}

	/// <summary> Skip value if 'stringBuilder' is null </summary>
	private static bool WriteValue(string json, StringBuilder stringBuilder, ref int index)
	{
		if (stringBuilder != null)
			stringBuilder.Length = 0;

		bool isStringValue	= (json[index] == '\"');
		bool isArrayValue	= (json[index] == '[');
		bool isObjectValue	= (json[index] == '{');

		if (!isStringValue && !isArrayValue && !isObjectValue)
			return false;

		if (isArrayValue && (stringBuilder != null))
			stringBuilder.Append('[');

		if (isObjectValue && (stringBuilder != null))
			stringBuilder.Append('{');

		index++;

		if (SkipWhiteSpaces(json, ref index) == json.Length)
			return false;

		int curlyBrackets = 0;
		int squareBrackets = 0;

		while (index < json.Length)
		{
			char ch = json[index];

			if (ch == '{')
			{
				curlyBrackets++;
			}
			else if (ch == '}')
			{
				if (isObjectValue && (curlyBrackets == 0) && (squareBrackets == 0))
				{
					if (stringBuilder != null)
						stringBuilder.Append('}');

					index++;
					return true;
				}

				curlyBrackets--;
			}
			else if (ch == '[')
			{
				squareBrackets++;
			}
			else if (ch == ']')
			{
				if (isArrayValue && (curlyBrackets == 0) && (squareBrackets == 0))
				{
					if (stringBuilder != null)
						stringBuilder.Append(']');

					index++;
					return true;
				}

				squareBrackets--;
			}
			else if (ch == '\"')
			{
				if (isStringValue && (curlyBrackets == 0) && (squareBrackets == 0))
				{
					index++;
					return true;
				}
			}

			if ((curlyBrackets < 0) || (squareBrackets < 0))
				return false;

			index++;

			if (stringBuilder != null)
				stringBuilder.Append(ch);
		}

		return false;
	}

	private static int IsKey(string json, string key, int keyStart, int keyLength, ref int index)
	{
		if (json[index] != '\"')
			return -1;

		index++;

		bool isKey = true;

		for (int i = keyStart; i < keyStart + keyLength; ++i)
		{
			if (index == json.Length)
				return -1;

			if ((json[index] == '\"') || (key[i] != json[index]))
			{
				isKey = false;
				break;
			}

			index++;
		}

		if (index == json.Length)
			return -1;

		if (isKey && json[index] == '\"')
		{
			index++;
			return 1;
		}

		// Skip key
		while ((index < json.Length) && (json[index] != '\"'))
			index++;

		if (index == json.Length)
			return -1;

		index++;
		return 0;
	}

	private static bool SetNextKey(string key, ref int keyStart, ref int keyLength)
	{
		int nextKeyStart = keyStart + keyLength;
		int nextKeyLength = 0;

		for (int i = nextKeyStart; i < key.Length; ++i)
		{
			if (key[i] == '.')
				break;

			nextKeyLength++;
		}

		if (nextKeyLength == 0)
			return false;

		keyStart = nextKeyStart;
		keyLength = nextKeyLength;

		return true;
	}

	private static int SkipWhiteSpaces(string str, ref int index)
	{
		while ((index < str.Length) && (str[index] == ' ' || str[index] == '\t' || str[index] == '\n' || str[index] == '\r'))
			index++;

		return index;
	}
}
