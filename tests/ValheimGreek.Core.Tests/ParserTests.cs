using System.Linq;
using Xunit;

namespace ValheimGreek.Core.Tests
{
    public class ParserTests
    {
        [Fact]
        public void GoVerb_UsesPresentLemma()
        {
            var words = Greek3WordParser.Load(RepoPaths.Greek3Words());
            var word = words.Single(entry => entry.Id == "verbs/иду πάω");
            Assert.Equal("иду", word.Prompt);
            Assert.Equal("πάω", word.Target);
        }

        [Fact]
        public void TreeNoun_StripsArticle()
        {
            var words = Greek3WordParser.Load(RepoPaths.Greek3Words());
            var word = words.Single(entry => entry.Id == "nouns/дерево δέντρο");
            Assert.Equal("дерево", word.Prompt);
            Assert.Equal("δέντρο", word.Target);
        }

        [Fact]
        public void SongsLessonsBlocksEssaysAndReadme_AreExcluded()
        {
            var words = Greek3WordParser.Load(RepoPaths.Greek3Words());
            Assert.DoesNotContain(words, entry => entry.Id.StartsWith("songs/"));
            Assert.DoesNotContain(words, entry => entry.Id.StartsWith("lessons/"));
            Assert.DoesNotContain(words, entry => entry.Id.StartsWith("blocks/"));
            Assert.DoesNotContain(words, entry => entry.Id.StartsWith("essays/"));
            Assert.DoesNotContain(words, entry => entry.Id.EndsWith("readme") || entry.Id.Contains("/readme"));
            Assert.True(words.Count >= 4);
        }
    }
}
