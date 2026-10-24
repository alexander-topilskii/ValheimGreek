using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ValheimGreek.Core.Tests
{
    public class QuizBuilderTests
    {
        [Fact]
        public void FourOptions_HaveOneCorrectAndNoDuplicateLemmas()
        {
            var words = new List<WordEntry>();
            for (var i = 0; i < 6; i++)
            {
                words.Add(new WordEntry
                {
                    Id = "w" + i,
                    Prompt = "слово" + i,
                    Target = "λέξη" + i
                });
            }

            Assert.True(QuizBuilder.TryCreate(words, new Random(1), out var question));
            Assert.Equal(4, question.Options.Count);
            var norms = question.Options.Select(GreekNormalizer.Normalize).ToList();
            Assert.Equal(norms.Count, norms.Distinct().Count());
            var source = words.Single(entry => entry.Id == question.WordId);
            Assert.Equal(source.Prompt, question.Prompt);
            Assert.Equal(
                GreekNormalizer.Normalize(source.Target),
                GreekNormalizer.Normalize(question.Options[question.CorrectIndex]));
        }

        [Fact]
        public void FewerThanFourDistinctLemmas_Fails()
        {
            var words = new List<WordEntry>
            {
                new WordEntry { Id = "a", Prompt = "а", Target = "άλφα" },
                new WordEntry { Id = "b", Prompt = "б", Target = "βήτα" },
                new WordEntry { Id = "c", Prompt = "в", Target = "γάμα" }
            };

            Assert.False(QuizBuilder.TryCreate(words, new Random(1), out _));
        }
    }
}
