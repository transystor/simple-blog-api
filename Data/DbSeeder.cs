using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleBlog.Api.Models;

namespace SimpleBlog.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var adminSettings = scope.ServiceProvider.GetRequiredService<IOptions<AdminSeedSettings>>().Value;

        var existingAdmin = await db.AdminUsers.FirstOrDefaultAsync(x => x.Email == adminSettings.Email);
        if (existingAdmin is null)
        {
            db.AdminUsers.Add(new AdminUser
            {
                Id = Guid.NewGuid(),
                Email = adminSettings.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminSettings.Password),
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await db.Articles.AnyAsync())
        {
            db.Articles.Add(new Article
            {
                Id = Guid.NewGuid(),
                Title = "Welcome to Simple Blog",
                Summary = "First seeded article.",
                Content = "This is the first published article. Replace it from the admin panel.",
                Slug = "welcome-to-simple-blog",
                Status = ArticleStatus.Published,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                PublishedAt = DateTime.UtcNow
            });
        }

        if (!await db.SiteSettings.AnyAsync())
        {
            db.SiteSettings.Add(new SiteSetting
            {
                Id = Guid.NewGuid(),
                SiteTitle = "Simple Blog",
                NavigationLabel = "Blog",
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }
}
