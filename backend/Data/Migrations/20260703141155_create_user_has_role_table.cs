using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoostingHub.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class create_user_has_role_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
               name: "user_has_roles",
               columns: table => new
               {
                   id = table.Column<int>(type: "int", nullable: false)
                       .Annotation("SqlServer:Identity", "1, 1"),
                   user_id = table.Column<int>(type: "int", nullable: false),
                   role_id = table.Column<int>(type: "int", nullable: false)
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_user_has_roles", x => x.id);
                   table.ForeignKey(
                       name: "FK_user_has_roles_roles_role_id",
                       column: x => x.role_id,
                       principalTable: "roles",
                       principalColumn: "id",
                       onDelete: ReferentialAction.Cascade);
                   table.ForeignKey(
                       name: "FK_user_has_roles_users_user_id",
                       column: x => x.user_id,
                       principalTable: "users",
                       principalColumn: "id",
                       onDelete: ReferentialAction.Cascade);
               });
            migrationBuilder.CreateIndex(
              name: "IX_user_has_roles_role_id",
              table: "user_has_roles",
              column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_has_roles_user_id_role_id",
                table: "user_has_roles",
                columns: new[] { "user_id", "role_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
               name: "user_has_roles");
        }
    }
}
