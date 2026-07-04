using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoostingHub.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class create_role_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
              name: "roles",
              columns: table => new
              {
                  id = table.Column<int>(type: "int", nullable: false)
                      .Annotation("SqlServer:Identity", "1, 1"),
                  role_title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                  description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                  created_at = table.Column<DateOnly>(type: "date", nullable: false)
              },
              constraints: table =>
              {
                  table.PrimaryKey("PK_roles", x => x.id);
              });

            migrationBuilder.CreateIndex(
            name: "IX_roles_role_title",
            table: "roles",
            column: "role_title",
            unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
