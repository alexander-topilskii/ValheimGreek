using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ValheimGreek.Core
{
    public static class Greek3WordParser
    {
        private static readonly HashSet<string> ExcludedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "songs", "lessons", "blocks", "essays"
        };

        public static bool ShouldExport(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            var normalized = relativePath.Replace('\\', '/');
            var name = Path.GetFileName(normalized);
            if (!name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                return false;
            if (name.Equals("readme.md", StringComparison.OrdinalIgnoreCase))
                return false;

            var slash = normalized.IndexOf('/');
            var top = slash >= 0 ? normalized.Substring(0, slash) : "";
            return !ExcludedRoots.Contains(top);
        }

        public static IReadOnlyList<WordEntry> Load(string wordsRoot)
        {
            if (string.IsNullOrWhiteSpace(wordsRoot))
                throw new ArgumentException("Words root is required.", nameof(wordsRoot));
            if (!Directory.Exists(wordsRoot))
                throw new DirectoryNotFoundException(wordsRoot);

            var list = new List<WordEntry>();
            foreach (var file in Directory.EnumerateFiles(wordsRoot, "*.md", SearchOption.AllDirectories))
            {
                var relative = RelativePath(wordsRoot, file);
                if (!ShouldExport(relative))
                    continue;

                var entry = ParseFile(file, relative);
                if (entry == null)
                    continue;
                list.Add(entry);
            }

            list.Sort(delegate (WordEntry left, WordEntry right)
            {
                return string.CompareOrdinal(left.Id, right.Id);
            });
            return list;
        }

        public static WordEntry? ParseFile(string filePath, string relativePath)
        {
            var id = relativePath.Replace('\\', '/');
            if (id.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                id = id.Substring(0, id.Length - 3);

            var raw = File.ReadAllText(filePath);
            var body = StripFrontmatter(raw);
            var sections = SplitSections(body);
            string? translation = null;
            List<string>? baseForms = null;

            foreach (var section in sections)
            {
                if (!section.Title.Equals("база", StringComparison.OrdinalIgnoreCase))
                    continue;
                ParseBase(section.Lines, out translation, out baseForms);
            }

            if (translation == null || baseForms == null)
                return null;

            var prompt = LemmaSelector.SelectPrompt(translation);
            var target = LemmaSelector.SelectTarget(baseForms);
            if (prompt.Length == 0 || target.Length == 0)
                return null;

            return new WordEntry
            {
                Id = id.Normalize(NormalizationForm.FormC),
                Prompt = prompt.Normalize(NormalizationForm.FormC),
                Target = target.Normalize(NormalizationForm.FormC)
            };
        }

        private static string RelativePath(string root, string file)
{
    var from = new Uri(AppendSlash(Path.GetFullPath(root)));
    var to = new Uri(Path.GetFullPath(file));
    var relative = Uri.UnescapeDataString(from.MakeRelativeUri(to).ToString());
    return relative.Replace('\\', '/');
}

private static string AppendSlash(string path)
{
    if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
        path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        return path;
    return path + Path.DirectorySeparatorChar;
}

private static string StripFrontmatter(string raw)
        {
            if (!raw.StartsWith("---", StringComparison.Ordinal))
                return raw;

            var end = raw.IndexOf("\n---", 3, StringComparison.Ordinal);
            if (end < 0)
                return raw;

            var after = end + 4;
            if (after < raw.Length && raw[after] == '\r')
                after++;
            if (after < raw.Length && raw[after] == '\n')
                after++;
            return after <= raw.Length ? raw.Substring(after) : "";
        }

        private static List<Section> SplitSections(string body)
        {
            var sections = new List<Section>();
            Section? current = null;
            var lines = body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("# ", StringComparison.Ordinal))
                {
                    current = new Section(line.Substring(2).Trim());
                    sections.Add(current);
                    continue;
                }

                if (current != null)
                    current.Lines.Add(line);
            }

            return sections;
        }

        private static void ParseBase(List<string> lines, out string translation, out List<string> baseForms)
        {
            var content = "";
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                    continue;
                content = trimmed;
                break;
            }

            var colon = content.IndexOf(':');
            if (colon < 0)
            {
                translation = content;
                baseForms = new List<string>();
                return;
            }

            translation = content.Substring(0, colon).Trim();
            var formsPart = content.Substring(colon + 1).Trim();
            baseForms = new List<string>();
            if (formsPart.Length == 0)
                return;

            foreach (var part in formsPart.Split('-'))
            {
                var form = part.Trim();
                if (form.Length > 0)
                    baseForms.Add(form);
            }
        }

        private sealed class Section
        {
            public Section(string title)
            {
                Title = title;
            }

            public string Title { get; }
            public List<string> Lines { get; } = new List<string>();
        }
    }
}
