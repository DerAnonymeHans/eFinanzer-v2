using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eFinanzer.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptDetailsAndBudgetPositionRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Npgsql cannot change enum labels, renaming keeps the existing receipts valid
            migrationBuilder.Sql("""ALTER TYPE payment_type RENAME VALUE 'card' TO 'bank';""");

            migrationBuilder.AddColumn<int>(
                name: "BudgetPositionId",
                table: "Receipts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReceiptAt",
                table: "Receipts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "Receiver",
                table: "Receipts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_BudgetPositionId",
                table: "Receipts",
                column: "BudgetPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_BudgetPositions_BudgetPositionId",
                table: "Receipts",
                column: "BudgetPositionId",
                principalTable: "BudgetPositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_BudgetPositions_BudgetPositionId",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_BudgetPositionId",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "BudgetPositionId",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ReceiptAt",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "Receiver",
                table: "Receipts");
        }
    }
}
