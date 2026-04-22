namespace SimpleBlog.Api.Dtos;

public record UpdateSiteSettingsRequest(string SiteTitle, List<HeaderLinkDto> HeaderLinks);
