using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoostingHub.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class create_wallet_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
              name: "wallets",
              columns: table => new
              {
                  id = table.Column<int>(type: "int", nullable: false)
                      .Annotation("SqlServer:Identity", "1, 1"),
                  user_id = table.Column<int>(type: "int", nullable: false),
                  balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                  pending_balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                  total_earned = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                  total_withdrawn = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                  currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                  updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                  created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
              },
              constraints: table =>
              {
                  table.PrimaryKey("PK_wallets", x => x.id);
                  table.ForeignKey(
                      name: "FK_wallets_users_user_id",
                      column: x => x.user_id,
                      principalTable: "users",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
              });
            migrationBuilder.CreateIndex(
               name: "IX_wallets_user_id",
               table: "wallets",
               column: "user_id",
               unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wallets");
        }
    }
}
