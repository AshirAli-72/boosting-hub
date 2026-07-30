using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoostingHub.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class create_website_settings_table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "website_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    site_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    logo_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    favicon_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    hero_title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    hero_subtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    hero_description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    about_title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    about_description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    support_email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    support_phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    footer_text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    footer_description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    twitter_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    linkedin_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_website_settings", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "website_settings");
        }
    }
}
