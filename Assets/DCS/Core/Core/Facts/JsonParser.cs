using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DynamicComponent
{
    public static class JsonParser
    {
        public static string ConvertToJsonString(Dictionary<string, string> facts)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");

            bool first = true;
            foreach (var pair in facts)
            {
                if (!first) sb.AppendLine(",");
                sb.Append("  \"");
                sb.Append(EscapeJsonString(pair.Key));
                sb.Append("\": ");
                sb.Append(pair.Value);
                first = false;
            }

            sb.AppendLine();
            sb.Append("}");
            return sb.ToString();
        }

        public static Dictionary<string, string> ParseJsonStringToDictionary(string json)
        {
            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(json))
                return result;

            string trimmed = json.Trim();
            if (trimmed == "{}" || !trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
                return result;

            int i = 1;
            int length = trimmed.Length - 1;

            try
            {
                while (i < length)
                {
                    while (i < length && (char.IsWhiteSpace(trimmed[i]) || trimmed[i] == ',')) i++;
                    if (i >= length) break;

                    if (trimmed[i] != '"') throw new FormatException("Expected quote character");
                    i++;
                    int keyStart = i;
                    while (i < length && trimmed[i] != '"') i++;
                    string key = trimmed.Substring(keyStart, i - keyStart);
                    i++;

                    while (i < length && char.IsWhiteSpace(trimmed[i])) i++;
                    if (trimmed[i] != ':') throw new FormatException("Expected colon character");
                    i++;

                    while (i < length && char.IsWhiteSpace(trimmed[i])) i++;

                    int valueStart = i;
                    if (trimmed[i] == '"')
                    {
                        i++;
                        while (i < length && trimmed[i] != '"') i++;
                        i++;
                    }
                    else if (trimmed[i] == '[')
                    {
                        while (i < length && trimmed[i] != ']') i++;
                        i++;
                    }
                    else
                    {
                        while (i < length && trimmed[i] != ',' && !char.IsWhiteSpace(trimmed[i]) && trimmed[i] != '}') i++;
                    }

                    string value = trimmed.Substring(valueStart, i - valueStart);
                    result[UnescapeJsonString(key)] = value;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to parse JSON facts: " + e.Message);
            }

            return result;
        }

        public static string ConvertToJson<T>(T value)
        {
            return value switch
            {
                null => "null",
                string s => "\"" + EscapeJsonString(s) + "\"",
                bool b => b ? "true" : "false",
                int i => i.ToString(),
                float f => f.ToString("0.0#####", System.Globalization.CultureInfo.InvariantCulture),
                Vector2 v2 => "[" + v2.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + v2.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]",
                Vector3 v3 => "[" + v3.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + v3.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + v3.z.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]",
                Color c => "[" + c.r.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + c.g.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + c.b.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + c.a.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]",
                _ => throw new NotSupportedException("JSON conversion not supported for type " + typeof(T).Name)
            };
        }

        public static T ConvertFromJson<T>(string jsonValue)
        {
            if (jsonValue == "null")
                return default;

            try
            {
                return typeof(T) switch
                {
                    Type t when t == typeof(string) => (T)(object)jsonValue.Trim('"'),
                    Type t when t == typeof(bool) => (T)(object)(jsonValue.Trim().ToLower() == "true"),
                    Type t when t == typeof(int) => (T)(object)int.Parse(jsonValue.Trim()),
                    Type t when t == typeof(float) => (T)(object)float.Parse(jsonValue.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                    Type t when t == typeof(Vector2) => (T)(object)ParseJsonVector2(jsonValue),
                    Type t when t == typeof(Vector3) => (T)(object)ParseJsonVector3(jsonValue),
                    Type t when t == typeof(Color) => (T)(object)ParseJsonColor(jsonValue),
                    _ => throw new NotSupportedException("JSON conversion not supported for type " + typeof(T).Name)
                };
            }
            catch (Exception e)
            {
                throw new FormatException("Failed to parse JSON value: " + jsonValue, e);
            }
        }

        private static Vector2 ParseJsonVector2(string jsonValue)
        {
            string trimmed = jsonValue.Trim().Trim('[', ']');
            string[] parts = trimmed.Split(',');
            if (parts.Length != 2) throw new FormatException("Invalid Vector2 format");

            return new Vector2(
                float.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture)
            );
        }

        private static Vector3 ParseJsonVector3(string jsonValue)
        {
            string trimmed = jsonValue.Trim().Trim('[', ']');
            string[] parts = trimmed.Split(',');
            if (parts.Length != 3) throw new FormatException("Invalid Vector3 format");

            return new Vector3(
                float.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(parts[2].Trim(), System.Globalization.CultureInfo.InvariantCulture)
            );
        }

        private static Color ParseJsonColor(string jsonValue)
        {
            string trimmed = jsonValue.Trim().Trim('[', ']');
            string[] parts = trimmed.Split(',');
            if (parts.Length != 4) throw new FormatException("Invalid Color format");

            return new Color(
                float.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(parts[2].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(parts[3].Trim(), System.Globalization.CultureInfo.InvariantCulture)
            );
        }

        public static string EscapeJsonString(string input)
        {
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        public static string UnescapeJsonString(string input)
        {
            return input.Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }
}
