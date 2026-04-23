namespace SimpleBlog.Api.Models;

public class ArticleView
{
    public Guid Id { get; set; }
    public Guid ArticleId { get; set; }
    public string VisitorId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
