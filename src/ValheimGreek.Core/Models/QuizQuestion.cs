using System.Collections.Generic;

namespace ValheimGreek.Core
{
    public sealed class QuizQuestion
    {
        public string WordId { get; set; } = "";
        public string Prompt { get; set; } = "";
        public IReadOnlyList<string> Options { get; set; } = new string[0];
        public int CorrectIndex { get; set; }
    }
}
