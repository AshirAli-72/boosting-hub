using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoostingHub.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class create_task_proof_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "task_proofs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    task_id = table.Column<int>(type: "int", nullable: false),
                    proof_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    proof_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_proofs", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_proofs_task_generate_task_id",
                        column: x => x.task_id,
                        principalTable: "task_generate",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_task_proofs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });
            migrationBuilder.CreateIndex(
                name: "IX_task_proofs_task_id",
                table: "task_proofs",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_proofs_user_id",
                table: "task_proofs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropTable(
                name: "task_proofs");
        }
    }
}
