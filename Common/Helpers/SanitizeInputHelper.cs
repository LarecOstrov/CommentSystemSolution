using Ganss.Xss;
using System.Text.RegularExpressions;

internal static class SanitizeInputHelper
{
    private static readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();

    static SanitizeInputHelper()
    {
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.Add("a");
        _sanitizer.AllowedTags.Add("code");
        _sanitizer.AllowedTags.Add("i");
        _sanitizer.AllowedTags.Add("strong");

        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("title");
        _sanitizer.AllowedAttributes.Add("rel");
    }

    public static string ClearText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = RemoveBbCode(text);

        text = _sanitizer.Sanitize(text);

        return text;
    }

    /// <summary>
    /// Removes all BBCode tags except the allowed ones.
    /// </summary>
    private static string RemoveBbCode(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        string pattern = @"\[(?!/?(a|code|i|strong))[^]]*]";
        return Regex.Replace(input, pattern, string.Empty, RegexOptions.IgnoreCase);
    }
}
