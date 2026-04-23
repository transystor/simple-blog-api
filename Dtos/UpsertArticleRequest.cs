using SimpleBlog.Api.Models;

namespace SimpleBlog.Api.Dtos;

public record UpsertArticleRequest(
    string Title,
    string Summary,
    string Content,
    string? Slug,
    ArticleStatus Status,
    List<string>? Tags);
