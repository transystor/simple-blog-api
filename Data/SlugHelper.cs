using System.Text.RegularExpressions;

namespace SimpleBlog.Api.Data;

public static class SlugHelper
{
    public static string Generate(string input)
    {
        var lower = input.Trim().ToLowerInvariant();
        var normalized = Regex.Replace(lower, "[^a-z0-9\\s-]", string.Empty);
        var collapsed = Regex.Replace(normalized, "\\s+", "-");
        return Regex.Replace(collapsed, "-+", "-").Trim('-');
    }
}
