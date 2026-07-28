using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoostingHub.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class create_manual_payment_proofs_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "manual_payment_proofs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<int>(type: "int", nullable: false),
                    paid_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    paid_voucher = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    submit_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    payment_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manual_payment_proofs", x => x.id);
                    table.ForeignKey(
                        name: "FK_manual_payment_proofs_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_manual_payment_proofs_order_id",
                table: "manual_payment_proofs",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_manual_payment_proofs_status",
                table: "manual_payment_proofs",
                column: "status");

            migrationBuilder.AddColumn<string>(
                name: "voucher_no",
                table: "orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_voucher_no",
                table: "orders",
                column: "voucher_no",
                unique: true,
                filter: "[voucher_no] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_voucher_no",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "voucher_no",
                table: "orders");

            migrationBuilder.DropTable(
                name: "manual_payment_proofs");
        }
    }
}
