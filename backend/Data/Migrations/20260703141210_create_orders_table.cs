using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoostingHub.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class create_orders_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
               name: "orders",
               columns: table => new
               {
                   id = table.Column<int>(type: "int", nullable: false)
                       .Annotation("SqlServer:Identity", "1, 1"),
                   full_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                   email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                   platform = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                   service = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                   social_media_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                   description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                   quantity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                   budget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                   status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                   created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                   currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_orders", x => x.id);
               });
            migrationBuilder.CreateIndex(
                name: "IX_orders_status",
                table: "orders",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                 name: "orders");
        }
    }
}
