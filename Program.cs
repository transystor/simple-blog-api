using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SimpleBlog.Api.Data;
using SimpleBlog.Api.Dtos;
using SimpleBlog.Api.Models;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AdminSeedSettings>(builder.Configuration.GetSection("Admin"));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
                  ?? throw new InvalidOperationException("JWT settings are missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});


static string HtmlEncode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
static string BuildArticleHtml(Article article, SiteSetting? settings, HttpRequest request)
{
    var siteTitle = settings?.SiteTitle?.Trim();
    var articleUrl = $"{request.Scheme}://{request.Host}{request.Path}";
    var previewImage = System.Text.RegularExpressions.Regex.Match(article.Content ?? string.Empty, "<img[^>]+src=\"([^\"]+)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value;
    var safeTitle = HtmlEncode(article.Title);
    var safeSummary = HtmlEncode(article.Summary);
    var safeSiteTitle = HtmlEncode(string.IsNullOrWhiteSpace(siteTitle) ? "круглог" : siteTitle);
    var published = article.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    var publishedLabel = article.PublishedAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? string.Empty;
    var imageMeta = string.IsNullOrWhiteSpace(previewImage)
        ? string.Empty
        : $"<meta property=\"og:image\" content=\"{HtmlEncode(previewImage)}\" />";

    var html = $$"""
<!doctype html>
<html lang="ru">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>{{safeTitle}}</title>
  <meta name="description" content="{{safeSummary}}" />
  <meta property="og:type" content="article" />
  <meta property="og:title" content="{{safeTitle}}" />
  <meta property="og:description" content="{{safeSummary}}" />
  <meta property="og:url" content="{{HtmlEncode(articleUrl)}}" />
  <meta property="og:site_name" content="{{safeSiteTitle}}" />
  <link rel="canonical" href="{{HtmlEncode(articleUrl)}}" />
  {{imageMeta}}
  <style>
    body { margin: 0; font-family: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background: #f3f4f6; color: #111827; }
    .container { max-width: 860px; margin: 0 auto; padding: 24px; }
    article { background: #fff; border-radius: 16px; padding: 28px; box-shadow: 0 8px 30px rgba(0,0,0,.06); }
    h1 { margin: 0 0 12px; font-size: 2.2rem; line-height: 1.1; }
    .meta { color: #6b7280; margin-bottom: 16px; }
    .summary { font-size: 1.05rem; color: #374151; margin-bottom: 20px; }
    .content { line-height: 1.6; }
    .content p { margin: 0 0 1em; line-height: 1.6; }
    .content img { max-width: 100%; height: auto; display: block; clear: both; float: none !important; }
    .content img[align='left'] { margin-left: 0; margin-right: auto; }
    .content img[align='center'] { margin-left: auto; margin-right: auto; }
    .content img[align='right'] { margin-left: auto; margin-right: 0; }
  </style>
</head>
<body>
  <div class="container">
    <article>
      <h1>{{safeTitle}}</h1>
      <div class="meta"><time datetime="{{published}}">{{publishedLabel}}</time></div>
      <div class="content">{{article.Content}}</div>
    </article>
  </div>
</body>
</html>
""";

    return html;
}

var app = builder.Build();
var uploadsDir = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads");
Directory.CreateDirectory(uploadsDir);

app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/auth/login", async (LoginRequest request, BlogDbContext db) =>
{
    var admin = await db.AdminUsers.FirstOrDefaultAsync(x => x.Email == request.Email);
    if (admin is null || !BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
    {
        return Results.Unauthorized();
    }

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, admin.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.Name, admin.Email),
        new Claim(ClaimTypes.Role, "Admin")
    };

    var credentials = new SigningCredentials(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: jwtSettings.Issuer,
        audience: jwtSettings.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: credentials);

    return Results.Ok(new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token)));
});

app.MapGet("/api/articles", async (string? tag, BlogDbContext db) =>
{
    var items = await db.Articles
        .Where(x => x.Status == ArticleStatus.Published)
        .OrderByDescending(x => x.PublishedAt)
        .ToListAsync();

    if (!string.IsNullOrWhiteSpace(tag))
    {
        var normalizedTag = tag.Trim().ToLowerInvariant();
        items = items
            .Where(x => x.GetTags().Any(t => t.Trim().ToLowerInvariant() == normalizedTag))
            .ToList();
    }

    return Results.Ok(items.Select(ArticleResponse.FromEntity));
});

app.MapGet("/api/articles/{slug}", async (string slug, BlogDbContext db) =>
{
    var item = await db.Articles
        .Where(x => x.Slug == slug && x.Status == ArticleStatus.Published)
        .Select(x => ArticleResponse.FromEntity(x))
        .FirstOrDefaultAsync();

    return item is null ? Results.NotFound() : Results.Ok(item);
});

app.MapGet("/api/site-settings", async (BlogDbContext db) =>
{
    var settings = await db.SiteSettings.OrderBy(x => x.UpdatedAt).FirstOrDefaultAsync();
    return settings is null ? Results.NotFound() : Results.Ok(SiteSettingsResponse.FromEntity(settings));
});

async Task<IResult> RenderArticlePage(string slug, HttpRequest request, BlogDbContext db)
{
    var article = await db.Articles
        .Where(x => x.Slug == slug && x.Status == ArticleStatus.Published)
        .FirstOrDefaultAsync();

    if (article is null)
    {
        return Results.NotFound();
    }

    var settings = await db.SiteSettings.OrderBy(x => x.UpdatedAt).FirstOrDefaultAsync();
    var html = BuildArticleHtml(article, settings, request);
    return Results.Content(html, "text/html; charset=utf-8");
}

app.MapGet("/iv/articles/{slug}", RenderArticlePage);
app.MapGet("/articles/{slug}", RenderArticlePage);

app.MapPost("/api/admin/upload-image", async (HttpRequest request) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { message = "Form data expected." });
    }

    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
    {
        return Results.BadRequest(new { message = "File is required." });
    }

    var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
    if (!allowedTypes.Contains(file.ContentType))
    {
        return Results.BadRequest(new { message = "Unsupported image type." });
    }

    var ext = Path.GetExtension(file.FileName);
    var fileName = $"{Guid.NewGuid()}{ext}";
    var path = Path.Combine(uploadsDir, fileName);

    await using var stream = File.Create(path);
    await file.CopyToAsync(stream);

    return Results.Ok(new UploadImageResponse($"/uploads/{fileName}"));
}).RequireAuthorization();

app.MapPut("/api/admin/site-settings", async (UpdateSiteSettingsRequest request, BlogDbContext db) =>
{
    var normalizedLinks = request.HeaderLinks
        .Select(x => new HeaderLinkDto(
            x.Label.Trim(),
            string.Equals(x.Type, "tag", StringComparison.OrdinalIgnoreCase) ? "tag" : "url",
            string.IsNullOrWhiteSpace(x.Value) ? "/" : x.Value.Trim(),
            x.Priority))
        .Where(x => !string.IsNullOrWhiteSpace(x.Label))
        .OrderBy(x => x.Priority)
        .ToList();

    if (normalizedLinks.Count == 0)
    {
        normalizedLinks.Add(new HeaderLinkDto("блог", "url", "/", 0));
    }

    var primaryLabel = normalizedLinks[0].Label;
    var headerLinksJson = System.Text.Json.JsonSerializer.Serialize(normalizedLinks);

    var settings = await db.SiteSettings.OrderBy(x => x.UpdatedAt).FirstOrDefaultAsync();
    if (settings is null)
    {
        settings = new SiteSetting
        {
            Id = Guid.NewGuid(),
            SiteTitle = request.SiteTitle.Trim(),
            NavigationLabel = primaryLabel,
            HeaderLinksJson = headerLinksJson,
            UpdatedAt = DateTime.UtcNow
        };
        db.SiteSettings.Add(settings);
    }
    else
    {
        settings.SiteTitle = request.SiteTitle.Trim();
        settings.NavigationLabel = primaryLabel;
        settings.HeaderLinksJson = headerLinksJson;
        settings.UpdatedAt = DateTime.UtcNow;
    }

    await db.SaveChangesAsync();
    return Results.Ok(SiteSettingsResponse.FromEntity(settings));
}).RequireAuthorization();

app.MapGet("/api/admin/articles", async (BlogDbContext db) =>
{
    var items = await db.Articles
        .OrderByDescending(x => x.UpdatedAt)
        .Select(x => ArticleResponse.FromEntity(x))
        .ToListAsync();

    return Results.Ok(items);
}).RequireAuthorization();

app.MapPost("/api/admin/articles", async (UpsertArticleRequest request, BlogDbContext db) =>
{
    var article = new Article
    {
        Id = Guid.NewGuid(),
        Title = request.Title.Trim(),
        Summary = request.Summary.Trim(),
        Content = request.Content.Trim(),
        Slug = string.IsNullOrWhiteSpace(request.Slug) ? SlugHelper.Generate(request.Title) : SlugHelper.Generate(request.Slug),
        Status = request.Status,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        PublishedAt = request.Status == ArticleStatus.Published ? DateTime.UtcNow : null,
        TagsJson = System.Text.Json.JsonSerializer.Serialize((request.Tags ?? [])
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList())
    };

    db.Articles.Add(article);
    await db.SaveChangesAsync();
    return Results.Created($"/api/admin/articles/{article.Id}", ArticleResponse.FromEntity(article));
}).RequireAuthorization();

app.MapPut("/api/admin/articles/{id:guid}", async (Guid id, UpsertArticleRequest request, BlogDbContext db) =>
{
    var article = await db.Articles.FirstOrDefaultAsync(x => x.Id == id);
    if (article is null) return Results.NotFound();

    article.Title = request.Title.Trim();
    article.Summary = request.Summary.Trim();
    article.Content = request.Content.Trim();
    article.Slug = string.IsNullOrWhiteSpace(request.Slug) ? SlugHelper.Generate(request.Title) : SlugHelper.Generate(request.Slug);
    article.Status = request.Status;
    article.TagsJson = System.Text.Json.JsonSerializer.Serialize((request.Tags ?? [])
        .Select(x => x.Trim())
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList());
    article.UpdatedAt = DateTime.UtcNow;
    article.PublishedAt = request.Status == ArticleStatus.Published ? article.PublishedAt ?? DateTime.UtcNow : null;

    await db.SaveChangesAsync();
    return Results.Ok(ArticleResponse.FromEntity(article));
}).RequireAuthorization();

app.MapDelete("/api/admin/articles/{id:guid}", async (Guid id, BlogDbContext db) =>
{
    var article = await db.Articles.FirstOrDefaultAsync(x => x.Id == id);
    if (article is null) return Results.NotFound();

    db.Articles.Remove(article);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.Run();
