using Microsoft.EntityFrameworkCore;
using SimpleBlog.Api.Models;

namespace SimpleBlog.Api.Data;

public class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleView> ArticleViews => Set<ArticleView>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TagsJson).HasColumnType("text").IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<ArticleView>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.VisitorId).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.ArticleId, x.VisitorId }).IsUnique();
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<SiteSetting>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SiteTitle).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NavigationLabel).HasMaxLength(100).IsRequired();
            entity.Property(x => x.HeaderLinksJson).HasColumnType("text").IsRequired();
        });
    }
}
