using System.IO;
using System.Linq;
using Xunit;

namespace ValheimGreek.Core.Tests
{
    public class DictionaryJsonTests
    {
        [Fact]
        public void RoundTrip_PreservesGreek()
        {
            var source = Greek3WordParser.Load(RepoPaths.Greek3Words());
            var path = Path.Combine(Path.GetTempPath(), "valheimgreek-dictionary-test.json");
            DictionaryJson.Write(path, source);
            var loaded = DictionaryJson.Read(path);
            Assert.Equal(source.Count, loaded.Count);
            var go = loaded.Single(entry => entry.Id == "verbs/иду πάω");
            Assert.Equal("иду", go.Prompt);
            Assert.Equal("πάω", go.Target);
        }
    }
}
