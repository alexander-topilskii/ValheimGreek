using System;
using System.Globalization;
using System.Text;

namespace ValheimGreek.Core
{
    public static class GreekNormalizer
    {
        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            var collapsed = CollapseWhitespace(value.Trim().ToLowerInvariant());
            var decomposed = collapsed.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;
                builder.Append(ch == 'ς' ? 'σ' : ch);
            }

            return builder.ToString();
        }

        public static string CollapseWhitespace(string value)
        {
            var builder = new StringBuilder(value.Length);
            var pendingSpace = false;
            foreach (var ch in value)
            {
                if (char.IsWhiteSpace(ch))
                {
                    pendingSpace = builder.Length > 0;
                    continue;
                }

                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(ch);
            }

            return builder.ToString();
        }
    }
}
