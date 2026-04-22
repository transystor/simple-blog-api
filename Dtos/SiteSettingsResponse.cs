using System.Text.Json;
using SimpleBlog.Api.Models;

namespace SimpleBlog.Api.Dtos;

public record SiteSettingsResponse(
    string SiteTitle,
    List<HeaderLinkDto> HeaderLinks,
    DateTime UpdatedAt)
{
    public static SiteSettingsResponse FromEntity(SiteSetting setting)
    {
        var links = string.IsNullOrWhiteSpace(setting.HeaderLinksJson)
            ? new List<HeaderLinkDto> { new(setting.NavigationLabel, "/") }
            : JsonSerializer.Deserialize<List<HeaderLinkDto>>(setting.HeaderLinksJson) ?? new List<HeaderLinkDto> { new(setting.NavigationLabel, "/") };

        if (links.Count == 0)
        {
            links.Add(new HeaderLinkDto(setting.NavigationLabel, "/"));
        }

        return new SiteSettingsResponse(setting.SiteTitle, links, setting.UpdatedAt);
    }
}
