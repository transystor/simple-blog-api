using SimpleBlog.Api.Models;

namespace SimpleBlog.Api.Dtos;

public record ArticleResponse(
    Guid Id,
    string Title,
    string Summary,
    string Content,
    string Slug,
    ArticleStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PublishedAt,
    List<string> Tags)
{
    public static ArticleResponse FromEntity(Article article) => new(
        article.Id,
        article.Title,
        article.Summary,
        article.Content,
        article.Slug,
        article.Status,
        article.CreatedAt,
        article.UpdatedAt,
        article.PublishedAt,
        article.GetTags());
}
