using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleBlog.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHeaderLinksToSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeaderLinksJson",
                table: "SiteSettings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeaderLinksJson",
                table: "SiteSettings");
        }
    }
}
