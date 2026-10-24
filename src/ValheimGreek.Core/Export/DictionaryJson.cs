using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ValheimGreek.Core
{
    public static class DictionaryJson
    {
        public static void Write(string path, IReadOnlyList<WordEntry> words)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (words == null)
                throw new ArgumentNullException(nameof(words));

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var builder = new StringBuilder();
            builder.Append("{\n  \"version\": 1,\n  \"words\": [\n");
            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                builder.Append("    {\"id\":\"");
                builder.Append(Escape(word.Id));
                builder.Append("\",\"prompt\":\"");
                builder.Append(Escape(word.Prompt));
                builder.Append("\",\"target\":\"");
                builder.Append(Escape(word.Target));
                builder.Append("\"}");
                if (i + 1 < words.Count)
                    builder.Append(',');
                builder.Append('\n');
            }

            builder.Append("  ]\n}\n");
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        }

        public static IReadOnlyList<WordEntry> Read(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var root = Parse(File.ReadAllText(path)) as JsonObject;
            if (root == null || !root.Values.TryGetValue("words", out var wordsValue))
                throw new InvalidDataException("dictionary.json has no words array.");

            var array = wordsValue as JsonArray;
            if (array == null)
                throw new InvalidDataException("dictionary.json words is not an array.");

            var list = new List<WordEntry>(array.Items.Count);
            foreach (var item in array.Items)
            {
                var obj = item as JsonObject;
                if (obj == null)
                    continue;
                var entry = new WordEntry
                {
                    Id = ReadString(obj, "id"),
                    Prompt = ReadString(obj, "prompt"),
                    Target = ReadString(obj, "target")
                };
                if (entry.Prompt.Length == 0 || entry.Target.Length == 0)
                    continue;
                list.Add(entry);
            }

            return list;
        }

        private static string ReadString(JsonObject obj, string name)
        {
            if (!obj.Values.TryGetValue(name, out var value))
                return "";
            var text = value as JsonString;
            return text == null ? "" : text.Value;
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var builder = new StringBuilder(value.Length + 8);
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (ch < ' ')
                            builder.Append("\\u").Append(((int)ch).ToString("x4"));
                        else
                            builder.Append(ch);
                        break;
                }
            }

            return builder.ToString();
        }

        private static JsonValue Parse(string json)
        {
            var parser = new Parser(json);
            var value = parser.ParseValue();
            parser.SkipWhitespace();
            if (!parser.End)
                throw new InvalidDataException("Trailing data in dictionary.json.");
            return value;
        }

        private sealed class Parser
        {
            private readonly string _text;
            private int _index;

            public Parser(string text)
            {
                _text = text ?? "";
            }

            public bool End => _index >= _text.Length;

            public JsonValue ParseValue()
            {
                SkipWhitespace();
                if (End)
                    throw new InvalidDataException("Unexpected end of dictionary.json.");

                var ch = _text[_index];
                if (ch == '{')
                    return ParseObject();
                if (ch == '[')
                    return ParseArray();
                if (ch == '"')
                    return new JsonString(ParseString());
                if (ch == 't' || ch == 'f')
                    return ParseBool();
                if (ch == 'n')
                    return ParseNull();
                return ParseNumber();
            }

            public void SkipWhitespace()
            {
                while (!End && char.IsWhiteSpace(_text[_index]))
                    _index++;
            }

            private JsonObject ParseObject()
            {
                Expect('{');
                var obj = new JsonObject();
                SkipWhitespace();
                if (Peek('}'))
                {
                    _index++;
                    return obj;
                }

                while (true)
                {
                    SkipWhitespace();
                    if (_text[_index] != '"')
                        throw new InvalidDataException("Object key must be a string.");
                    var key = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    obj.Values[key] = ParseValue();
                    SkipWhitespace();
                    if (Peek('}'))
                    {
                        _index++;
                        return obj;
                    }

                    Expect(',');
                }
            }

            private JsonArray ParseArray()
            {
                Expect('[');
                var array = new JsonArray();
                SkipWhitespace();
                if (Peek(']'))
                {
                    _index++;
                    return array;
                }

                while (true)
                {
                    array.Items.Add(ParseValue());
                    SkipWhitespace();
                    if (Peek(']'))
                    {
                        _index++;
                        return array;
                    }

                    Expect(',');
                }
            }

            private string ParseString()
            {
                Expect('"');
                var builder = new StringBuilder();
                while (!End)
                {
                    var ch = _text[_index++];
                    if (ch == '"')
                        return builder.ToString();
                    if (ch != '\\')
                    {
                        builder.Append(ch);
                        continue;
                    }

                    if (End)
                        throw new InvalidDataException("Broken escape in dictionary.json.");
                    var escaped = _text[_index++];
                    switch (escaped)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            builder.Append(escaped);
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'u':
                            if (_index + 4 > _text.Length)
                                throw new InvalidDataException("Broken unicode escape in dictionary.json.");
                            builder.Append((char)Convert.ToInt32(_text.Substring(_index, 4), 16));
                            _index += 4;
                            break;
                        default:
                            throw new InvalidDataException("Unknown escape in dictionary.json.");
                    }
                }

                throw new InvalidDataException("Unclosed string in dictionary.json.");
            }

            private JsonBool ParseBool()
            {
                if (_text.IndexOf("true", _index, StringComparison.Ordinal) == _index)
                {
                    _index += 4;
                    return new JsonBool(true);
                }

                if (_text.IndexOf("false", _index, StringComparison.Ordinal) == _index)
                {
                    _index += 5;
                    return new JsonBool(false);
                }

                throw new InvalidDataException("Invalid boolean in dictionary.json.");
            }

            private JsonNull ParseNull()
            {
                if (_text.IndexOf("null", _index, StringComparison.Ordinal) != _index)
                    throw new InvalidDataException("Invalid null in dictionary.json.");
                _index += 4;
                return new JsonNull();
            }

            private JsonNumber ParseNumber()
            {
                var start = _index;
                while (!End)
                {
                    var ch = _text[_index];
                    if ((ch < '0' || ch > '9') && ch != '-' && ch != '+' && ch != '.' && ch != 'e' && ch != 'E')
                        break;
                    _index++;
                }

                if (start == _index)
                    throw new InvalidDataException("Invalid number in dictionary.json.");
                return new JsonNumber(_text.Substring(start, _index - start));
            }

            private void Expect(char ch)
            {
                if (End || _text[_index] != ch)
                    throw new InvalidDataException("Expected '" + ch + "' in dictionary.json.");
                _index++;
            }

            private bool Peek(char ch)
            {
                return !End && _text[_index] == ch;
            }
        }

        private abstract class JsonValue
        {
        }

        private sealed class JsonObject : JsonValue
        {
            public Dictionary<string, JsonValue> Values { get; } = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
        }

        private sealed class JsonArray : JsonValue
        {
            public List<JsonValue> Items { get; } = new List<JsonValue>();
        }

        private sealed class JsonString : JsonValue
        {
            public JsonString(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        private sealed class JsonNumber : JsonValue
        {
            public JsonNumber(string raw)
            {
                Raw = raw;
            }

            public string Raw { get; }
        }

        private sealed class JsonBool : JsonValue
        {
            public JsonBool(bool value)
            {
                Value = value;
            }

            public bool Value { get; }
        }

        private sealed class JsonNull : JsonValue
        {
        }
    }
}
