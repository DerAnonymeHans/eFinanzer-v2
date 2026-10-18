using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eFinanzer.Migrations
{
    /// <inheritdoc />
    public partial class SwitchedToFiscalYearComposedPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_FiscalYears_FiscalYearKey",
                table: "Budgets");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_FiscalYears_FiscalYearKey",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_FiscalYearKey_Key",
                table: "Receipts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FiscalYears",
                table: "FiscalYears");

            migrationBuilder.DropIndex(
                name: "IX_FiscalYears_AccountingUnitKey",
                table: "FiscalYears");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_FiscalYearKey",
                table: "Budgets");

            migrationBuilder.AlterColumn<string>(
                name: "FiscalYearKey",
                table: "Receipts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15);

            migrationBuilder.AddColumn<string>(
                name: "AccountingUnitKey",
                table: "Receipts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "FiscalYears",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15);

            migrationBuilder.AlterColumn<string>(
                name: "FiscalYearKey",
                table: "Budgets",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15);

            migrationBuilder.AddColumn<string>(
                name: "AccountingUnitKey",
                table: "Budgets",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FiscalYears",
                table: "FiscalYears",
                columns: new[] { "AccountingUnitKey", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_AccountingUnitKey_FiscalYearKey_Key",
                table: "Receipts",
                columns: new[] { "AccountingUnitKey", "FiscalYearKey", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_AccountingUnitKey_FiscalYearKey",
                table: "Budgets",
                columns: new[] { "AccountingUnitKey", "FiscalYearKey" });

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_FiscalYears_AccountingUnitKey_FiscalYearKey",
                table: "Budgets",
                columns: new[] { "AccountingUnitKey", "FiscalYearKey" },
                principalTable: "FiscalYears",
                principalColumns: new[] { "AccountingUnitKey", "Key" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_AccountingUnits_AccountingUnitKey",
                table: "Receipts",
                column: "AccountingUnitKey",
                principalTable: "AccountingUnits",
                principalColumn: "Key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_FiscalYears_AccountingUnitKey_FiscalYearKey",
                table: "Receipts",
                columns: new[] { "AccountingUnitKey", "FiscalYearKey" },
                principalTable: "FiscalYears",
                principalColumns: new[] { "AccountingUnitKey", "Key" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_FiscalYears_AccountingUnitKey_FiscalYearKey",
                table: "Budgets");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_AccountingUnits_AccountingUnitKey",
                table: "Receipts");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_FiscalYears_AccountingUnitKey_FiscalYearKey",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_AccountingUnitKey_FiscalYearKey_Key",
                table: "Receipts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FiscalYears",
                table: "FiscalYears");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_AccountingUnitKey_FiscalYearKey",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "AccountingUnitKey",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "AccountingUnitKey",
                table: "Budgets");

            migrationBuilder.AlterColumn<string>(
                name: "FiscalYearKey",
                table: "Receipts",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "FiscalYears",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "FiscalYearKey",
                table: "Budgets",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FiscalYears",
                table: "FiscalYears",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_FiscalYearKey_Key",
                table: "Receipts",
                columns: new[] { "FiscalYearKey", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_AccountingUnitKey",
                table: "FiscalYears",
                column: "AccountingUnitKey");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_FiscalYearKey",
                table: "Budgets",
                column: "FiscalYearKey");

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_FiscalYears_FiscalYearKey",
                table: "Budgets",
                column: "FiscalYearKey",
                principalTable: "FiscalYears",
                principalColumn: "Key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_FiscalYears_FiscalYearKey",
                table: "Receipts",
                column: "FiscalYearKey",
                principalTable: "FiscalYears",
                principalColumn: "Key",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
