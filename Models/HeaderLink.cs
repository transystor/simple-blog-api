namespace SimpleBlog.Api.Models;

public class HeaderLink
{
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "url";
    public string Value { get; set; } = "/";
}
