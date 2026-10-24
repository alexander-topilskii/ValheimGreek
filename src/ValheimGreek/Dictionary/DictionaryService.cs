using System;
using System.Collections.Generic;
using System.IO;
using ValheimGreek.Core;

namespace ValheimGreek
{
    public static class DictionaryService
    {
        public static IReadOnlyList<WordEntry> Words { get; private set; } = new WordEntry[0];

        public static bool CanQuiz => Words.Count >= 4;

        public static string ResolvePath(string pluginDirectory, string configured)
        {
            if (string.IsNullOrWhiteSpace(configured))
                configured = "dictionary.json";
            if (Path.IsPathRooted(configured))
                return configured;
            return Path.Combine(pluginDirectory, configured);
        }

        public static void Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Dictionary was not found.", path);

            var words = DictionaryJson.Read(path);
            if (words.Count < 4)
                throw new InvalidDataException("Dictionary has fewer than 4 usable words.");

            Words = words;
        }
    }
}
