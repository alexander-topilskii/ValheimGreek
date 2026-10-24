using System;
using System.Collections.Generic;

namespace ValheimGreek.Core
{
    public static class LemmaSelector
    {
        private static readonly string[] Articles =
        {
            "στον", "στην", "στους", "στις", "τους", "της", "των",
            "τον", "την", "στη", "στο", "στα", "του", "τις",
            "οι", "τα", "το", "τη", "ο", "η"
        };

        public static string SelectPrompt(string translation)
        {
            if (string.IsNullOrWhiteSpace(translation))
                return "";

            var slash = translation.IndexOf('/');
            var first = slash >= 0 ? translation.Substring(0, slash) : translation;
            return GreekNormalizer.CollapseWhitespace(first.Trim());
        }

        public static string SelectTarget(IReadOnlyList<string> baseForms)
        {
            if (baseForms == null || baseForms.Count == 0)
                return "";

            var forms = new List<string>(baseForms.Count);
            foreach (var form in baseForms)
            {
                if (string.IsNullOrWhiteSpace(form))
                    continue;
                forms.Add(form.Trim());
            }

            if (forms.Count == 0)
                return "";

            string chosen;
            if (forms.Count >= 2 && IsFuture(forms[forms.Count - 1]))
                chosen = forms.Count >= 3 ? forms[1] : forms[0];
            else
                chosen = forms[0];

            return StripLeadingArticle(chosen);
        }

        public static string StripLeadingArticle(string form)
        {
            var trimmed = GreekNormalizer.CollapseWhitespace(form.Trim());
            var lower = trimmed.ToLowerInvariant();
            foreach (var article in Articles)
            {
                var prefix = article + " ";
                if (!lower.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                return trimmed.Substring(prefix.Length).Trim();
            }

            return trimmed;
        }

        private static bool IsFuture(string form)
        {
            var lower = form.Trim().ToLowerInvariant();
            return lower.Equals("θα", StringComparison.Ordinal) || lower.StartsWith("θα ", StringComparison.Ordinal);
        }
    }
}
