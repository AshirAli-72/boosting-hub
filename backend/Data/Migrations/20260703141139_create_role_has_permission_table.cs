using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoostingHub.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class create_role_has_permission_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
               name: "roles_has_permissions",
               columns: table => new
               {
                   id = table.Column<int>(type: "int", nullable: false)
                       .Annotation("SqlServer:Identity", "1, 1"),
                   role_id = table.Column<int>(type: "int", nullable: false),
                   permission_id = table.Column<int>(type: "int", nullable: false)
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_roles_has_permissions", x => x.id);
                   table.ForeignKey(
                       name: "FK_roles_has_permissions_permissions_permission_id",
                       column: x => x.permission_id,
                       principalTable: "permissions",
                       principalColumn: "id",
                       onDelete: ReferentialAction.Cascade);
                   table.ForeignKey(
                       name: "FK_roles_has_permissions_roles_role_id",
                       column: x => x.role_id,
                       principalTable: "roles",
                       principalColumn: "id",
                       onDelete: ReferentialAction.Cascade);
               });
            migrationBuilder.CreateIndex(
            name: "IX_roles_has_permissions_permission_id",
            table: "roles_has_permissions",
            column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_has_permissions_role_id_permission_id",
                table: "roles_has_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
               name: "roles_has_permissions");
        }
    }
}
