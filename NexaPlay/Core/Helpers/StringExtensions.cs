using System.Globalization;
using System.Text;

namespace NexaPlay.Core.Helpers;

public static class StringExtensions
{
    public static string NormalizeForSearch(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(normalized.Length);
        var pendingSpace = false;

        foreach (var rune in normalized.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark)
                continue;

            if (rune.Value is '\'' or 0x2018 or 0x2019 or '`' or 0x00B4)
                continue;

            if (Rune.IsLetterOrDigit(rune))
            {
                if (pendingSpace && result.Length > 0)
                    result.Append(' ');
                result.Append(Rune.ToLowerInvariant(rune));
                pendingSpace = false;
            }
            else
            {
                pendingSpace = result.Length > 0;
            }
        }

        return result.ToString();
    }
}
