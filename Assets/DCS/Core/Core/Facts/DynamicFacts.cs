using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DynamicComponent
{ 
    [Serializable]
    public class DynamicFacts : BaseFacts, ISerializationCallbackReceiver
    {
        [SerializeField, TextArea(3, 20)]
        private string jsonData = "{}";

        private Dictionary<string, string> facts = new Dictionary<string, string>();

        // ==================== SERIALIZATION ====================

        public void OnBeforeSerialize()
        {
            jsonData = ConvertToJsonString();
        }

        public void OnAfterDeserialize()
        {
            facts = ParseJsonStringToDictionary(jsonData);
        }

        private string ConvertToJsonString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");

            bool first = true;
            foreach (var pair in facts)
            {
                if (!first) sb.AppendLine(",");
                sb.Append($"  \"{EscapeJsonString(pair.Key)}\": {pair.Value}");
                first = false;
            }

            sb.AppendLine();
            sb.Append("}");
            return sb.ToString();
        }

        private Dictionary<string, string> ParseJsonStringToDictionary(string json)
        {
            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(json))
                return result;

            string trimmed = json.Trim();
            if (trimmed == "{}" || !trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
                return result;

            int i = 1; // Пропускаем открывающую фигурную скобку '{'
            int length = trimmed.Length - 1; // Не доходим до закрывающей '}'

            try
            {
                while (i < length)
                {
                    // 1. Пропускаем пробелы, переносы строк и запятые перед ключом
                    while (i < length && (char.IsWhiteSpace(trimmed[i]) || trimmed[i] == ',')) i++;
                    if (i >= length) break;

                    // 2. Читаем КЛЮЧ (он обязан начинаться с кавычки)
                    if (trimmed[i] != '"') throw new FormatException($"Expected '\"' at position {i}");
                    i++; // Пропускаем открывающую кавычку ключа
                    int keyStart = i;
                    while (i < length && trimmed[i] != '"') i++; // Ищем закрывающую кавычку
                    string key = trimmed.Substring(keyStart, i - keyStart);
                    i++; // Пропускаем закрывающую кавычку ключа

                    // 3. Ищем двоеточие ':' между ключом и значением
                    while (i < length && char.IsWhiteSpace(trimmed[i])) i++;
                    if (trimmed[i] != ':') throw new FormatException($"Expected ':' after key '{key}' at position {i}");
                    i++; // Пропускаем двоеточие

                    // 4. Пропускаем пробелы перед значением
                    while (i < length && char.IsWhiteSpace(trimmed[i])) i++;

                    // 5. Читаем ЗНАЧЕНИЕ (гетерогенное: строка, число, массив или булево)
                    int valueStart = i;
                    if (trimmed[i] == '"') // Если значение — строка в кавычках
                    {
                        i++; // Пропускаем открывающую кавычку
                        while (i < length && trimmed[i] != '"') i++; // Ищем закрывающую кавычку
                        i++; // Включаем закрывающую кавычку в подстроку
                    }
                    else if (trimmed[i] == '[') // Если значение — массив (Vector2, Vector3, Color)
                    {
                        while (i < length && trimmed[i] != ']') i++; // Ищем закрывающую квадратную скобку
                        i++; // Включаем скобку в подстроку
                    }
                    else // Если значение — примитив (число, bool, null)
                    {
                        // Читаем до ближайшей запятой, пробела или конца JSON
                        while (i < length && trimmed[i] != ',' && !char.IsWhiteSpace(trimmed[i]) && trimmed[i] != '}') i++;
                    }

                    string value = trimmed.Substring(valueStart, i - valueStart);

                    // Сохраняем чистый ключ и сырое JSON-значение в словарь
                    result[UnescapeJsonString(key)] = value;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to parse JSON facts (position {i}): {e.Message}");
            }

            return result;
        }

        // ==================== VALUE CONVERSION ====================

        protected override bool TryGetInternal<T>(string name, out T value)
        {
            value = default;

            if (facts.TryGetValue(name, out string jsonValue) && !string.IsNullOrEmpty(jsonValue))
            {
                try
                {
                    value = ConvertFromJson<T>(jsonValue);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to convert fact '{name}': {jsonValue} -> {typeof(T).Name}: {e.Message}");
                }
            }

            return false;
        }

        protected override void SetInternal<T>(string name, T value)
        {
            facts[name] = ConvertToJson(value);
        }

        // ==================== JSON CONVERSION METHODS ====================

        private string ConvertToJson<T>(T value)
        {
            return value switch
            {
                null => "null",
                string s => $"\"{EscapeJsonString(s)}\"",
                bool b => b ? "true" : "false",
                int i => i.ToString(),
                float f => f.ToString("0.0#####"), // Максимальная точность
                Vector2 v2 => $"[{v2.x}, {v2.y}]",
                Vector3 v3 => $"[{v3.x}, {v3.y}, {v3.z}]",
                Color c => $"[{c.r}, {c.g}, {c.b}, {c.a}]",
                _ => throw new NotSupportedException($"JSON conversion not supported for type {typeof(T).Name}")
            };
        }

        private T ConvertFromJson<T>(string jsonValue)
        {
            if (jsonValue == "null")
                return default;

            try
            {
                return typeof(T) switch
                {
                    Type t when t == typeof(string) => (T)(object)ParseJsonString(jsonValue),
                    Type t when t == typeof(bool) => (T)(object)ParseJsonBool(jsonValue),
                    Type t when t == typeof(int) => (T)(object)ParseJsonInt(jsonValue),
                    Type t when t == typeof(float) => (T)(object)ParseJsonFloat(jsonValue),
                    Type t when t == typeof(Vector2) => (T)(object)ParseJsonVector2(jsonValue),
                    Type t when t == typeof(Vector3) => (T)(object)ParseJsonVector3(jsonValue),
                    Type t when t == typeof(Color) => (T)(object)ParseJsonColor(jsonValue),
                    _ => throw new NotSupportedException($"JSON conversion not supported for type {typeof(T).Name}")
                };
            }
            catch (Exception e)
            {
                throw new FormatException($"Failed to parse JSON value: {jsonValue} as {typeof(T).Name}", e);
            }
        }

        // ==================== JSON PARSING METHODS ====================

        private string ParseJsonString(string jsonValue)
        {
            return jsonValue.Trim('"');
        }

        private bool ParseJsonBool(string jsonValue)
        {
            return jsonValue.Trim().ToLower() == "true";
        }

        private int ParseJsonInt(string jsonValue)
        {
            return int.Parse(jsonValue.Trim());
        }

        private float ParseJsonFloat(string jsonValue)
        {
            return float.Parse(jsonValue.Trim());
        }

        private Vector2 ParseJsonVector2(string jsonValue)
        {
            var trimmed = jsonValue.Trim().Trim('[', ']');
            var parts = trimmed.Split(',');
            if (parts.Length != 2) throw new FormatException("Invalid Vector2 format");

            return new Vector2(
                float.Parse(parts[0].Trim()),
                float.Parse(parts[1].Trim())
            );
        }

        private Vector3 ParseJsonVector3(string jsonValue)
        {
            var trimmed = jsonValue.Trim().Trim('[', ']');
            var parts = trimmed.Split(',');
            if (parts.Length != 3) throw new FormatException("Invalid Vector3 format");

            return new Vector3(
                float.Parse(parts[0].Trim()),
                float.Parse(parts[1].Trim()),
                float.Parse(parts[2].Trim())
            );
        }

        private Color ParseJsonColor(string jsonValue)
        {
            var trimmed = jsonValue.Trim().Trim('[', ']');
            var parts = trimmed.Split(',');
            if (parts.Length != 4) throw new FormatException("Invalid Color format");

            return new Color(
                float.Parse(parts[0].Trim()),
                float.Parse(parts[1].Trim()),
                float.Parse(parts[2].Trim()),
                float.Parse(parts[3].Trim())
            );
        }

        // ==================== STRING ESCAPING ====================

        private string EscapeJsonString(string input)
        {
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private string UnescapeJsonString(string input)
        {
            return input.Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        // ==================== REMAINING METHODS ====================

        protected override bool RemoveInternal(string name)
        {
            return facts.Remove(name);
        }

        protected override bool ContainsInternal(string name)
        {
            return facts.ContainsKey(name);
        }

        protected override void ClearInternal()
        {
            facts.Clear();
        }

        public override string ToString()
        {
            return ConvertToJsonString();
        }
}
}