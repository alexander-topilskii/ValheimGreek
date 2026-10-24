using System;
using ValheimGreek.Core;

namespace ValheimGreek.Export
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: ValheimGreek.Export <greek3-words-root> <output.json>");
                return 1;
            }

            var words = Greek3WordParser.Load(args[0]);
            if (words.Count < 4)
            {
                Console.Error.WriteLine("Need at least 4 words with a prompt and a lemma, got " + words.Count + ".");
                return 1;
            }

            DictionaryJson.Write(args[1], words);
            Console.WriteLine("Wrote " + words.Count + " words to " + args[1]);
            return 0;
        }
    }
}
