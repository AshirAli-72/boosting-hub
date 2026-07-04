using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoostingHub.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class create_task_complete_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "task_complete",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    task_id = table.Column<int>(type: "int", nullable: false),
                    proof_id = table.Column<int>(type: "int", nullable: true),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_complete", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_complete_task_generate_task_id",
                        column: x => x.task_id,
                        principalTable: "task_generate",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_task_complete_task_proofs_proof_id",
                        column: x => x.proof_id,
                        principalTable: "task_proofs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_task_complete_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_task_complete_proof_id",
                table: "task_complete",
                column: "proof_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_complete_task_id",
                table: "task_complete",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_complete_user_id",
                table: "task_complete",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_complete");
        }
    }
}
