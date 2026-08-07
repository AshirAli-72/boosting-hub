using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoostingHub.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class create_user_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    password = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    email_verified_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remember_token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });
   
  
            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true,
                filter: "[email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_phone",
                table: "users",
                column: "phone",
                unique: true,
                filter: "[phone] IS NOT NULL");

           
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
           
           migrationBuilder.DropTable(
                name: "users");
        }
   }
}
