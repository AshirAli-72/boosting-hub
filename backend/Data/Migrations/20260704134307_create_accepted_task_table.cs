using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoostingHub.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class create_accepted_task_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accepted_tasks",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    task_id = table.Column<int>(type: "int", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accepted_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_accepted_tasks_task_generate_task_id",
                        column: x => x.task_id,
                        principalTable: "task_generate",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_accepted_tasks_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_accepted_tasks_task_id",
                table: "accepted_tasks",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_accepted_tasks_user_id",
                table: "accepted_tasks",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_accepted_tasks_user_id_task_id",
                table: "accepted_tasks",
                columns: new[] { "user_id", "task_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accepted_tasks");
        }
    }
}
