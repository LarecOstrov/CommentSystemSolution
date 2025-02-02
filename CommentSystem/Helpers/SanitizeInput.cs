using Ganss.Xss;

namespace CommentSystem.Helpers;

internal static class SanitizeInput
{
    private static readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();

    static SanitizeInput()
    {
        _sanitizer.AllowedTags.Add("a");
        _sanitizer.AllowedTags.Add("code");
        _sanitizer.AllowedTags.Add("i");
        _sanitizer.AllowedTags.Add("strong");
    }

    public static string ClearText(string text)
    {
        return _sanitizer.Sanitize(text);
    }
}
