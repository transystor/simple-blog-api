using SimpleBlog.Api.Models;

namespace SimpleBlog.Api.Dtos;

public record SiteSettingsResponse(
    string SiteTitle,
    string NavigationLabel,
    DateTime UpdatedAt)
{
    public static SiteSettingsResponse FromEntity(SiteSetting setting) => new(
        setting.SiteTitle,
        setting.NavigationLabel,
        setting.UpdatedAt);
}
