using System;
using System.Collections.Generic;

namespace ValheimGreek.Core
{
    public static class QuizBuilder
    {
        public static bool TryCreate(IReadOnlyList<WordEntry> words, Random random, out QuizQuestion question)
        {
            if (words == null)
                throw new ArgumentNullException(nameof(words));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            question = new QuizQuestion();
            var usable = new List<WordEntry>();
            foreach (var word in words)
            {
                if (word == null)
                    continue;
                if (string.IsNullOrWhiteSpace(word.Prompt) || string.IsNullOrWhiteSpace(word.Target))
                    continue;
                usable.Add(word);
            }

            Shuffle(usable, random);
            foreach (var candidate in usable)
            {
                var correctNorm = GreekNormalizer.Normalize(candidate.Target);
                if (correctNorm.Length == 0)
                    continue;

                var distractors = new List<WordEntry>(3);
                var seen = new HashSet<string>(StringComparer.Ordinal) { correctNorm };
                foreach (var other in usable)
                {
                    if (ReferenceEquals(other, candidate))
                        continue;
                    var norm = GreekNormalizer.Normalize(other.Target);
                    if (norm.Length == 0 || !seen.Add(norm))
                        continue;
                    distractors.Add(other);
                    if (distractors.Count == 3)
                        break;
                }

                if (distractors.Count < 3)
                    continue;

                var options = new List<string>(4)
                {
                    candidate.Target,
                    distractors[0].Target,
                    distractors[1].Target,
                    distractors[2].Target
                };
                Shuffle(options, random);
                var correctIndex = -1;
                for (var i = 0; i < options.Count; i++)
                {
                    if (!string.Equals(options[i], candidate.Target, StringComparison.Ordinal))
                        continue;
                    correctIndex = i;
                    break;
                }

                if (correctIndex < 0)
                    continue;

                question = new QuizQuestion
                {
                    WordId = candidate.Id,
                    Prompt = candidate.Prompt,
                    Options = options,
                    CorrectIndex = correctIndex
                };
                return true;
            }

            return false;
        }

        private static void Shuffle<T>(IList<T> items, Random random)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var swap = random.Next(i + 1);
                var temp = items[i];
                items[i] = items[swap];
                items[swap] = temp;
            }
        }
    }
}
