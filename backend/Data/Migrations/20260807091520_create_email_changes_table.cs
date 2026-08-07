using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoostingHub.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class create_email_changes_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_changes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    old_email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    new_email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    otp_code = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    is_used = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_changes", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_changes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_changes_user_id",
                table: "email_changes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_changes_user_id_is_used",
                table: "email_changes",
                columns: new[] { "user_id", "is_used" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_changes");
        }
    }
}
