namespace SimpleBlog.Api.Models;

public class SiteSetting
{
    public Guid Id { get; set; }
    public string SiteTitle { get; set; } = "Simple Blog";
    public string NavigationLabel { get; set; } = "Blog";
    public string HeaderLinksJson { get; set; } = "[{\"label\":\"Blog\",\"url\":\"/\"}]";
    public DateTime UpdatedAt { get; set; }
}
