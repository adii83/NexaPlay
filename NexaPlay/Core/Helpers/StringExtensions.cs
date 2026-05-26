using System.Text.RegularExpressions;

namespace NexaPlay.Core.Helpers;

public static class StringExtensions
{
    public static string NormalizeForSearch(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Hapus karakter petik dan trademark agar Assassin's Creed™ menjadi Assassins Creed
        var result = input.Replace("'", "")
                          .Replace("’", "")
                          .Replace("™", "")
                          .Replace("®", "")
                          .Replace(":", "");
                          
        // Ganti dash dengan spasi agar Spider-Man bisa dicari dengan Spider Man
        result = result.Replace("-", " ");

        // Hapus spasi berlebih
        result = Regex.Replace(result, @"\s+", " ");
        
        return result.Trim().ToLowerInvariant();
    }
}
