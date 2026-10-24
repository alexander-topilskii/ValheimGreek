using Xunit;

namespace ValheimGreek.Core.Tests
{
    public class NormalizerTests
    {
        [Fact]
        public void TonosAndCase_Collapse()
        {
            Assert.Equal(GreekNormalizer.Normalize("δέΝτρο"), GreekNormalizer.Normalize("δεντρο"));
        }

        [Fact]
        public void FinalSigma_MatchesMedial()
        {
            Assert.Equal(GreekNormalizer.Normalize("λόγος"), GreekNormalizer.Normalize("λογοσ"));
        }
    }
}
