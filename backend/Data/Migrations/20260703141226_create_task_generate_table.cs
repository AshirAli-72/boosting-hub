using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoostingHub.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class create_task_generate_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "task_generate",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<int>(type: "int", nullable: false),
                    platform = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    service = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    reward = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_generate", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_generate_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_task_generate_order_id",
                table: "task_generate",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_generate_status",
                table: "task_generate",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "task_generate");
        }
    }
}
