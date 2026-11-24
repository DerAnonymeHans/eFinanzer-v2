using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using eFinanzer.Common.Database.Models;

#nullable disable

namespace eFinanzer.Migrations
{
    /// <inheritdoc />
    public partial class InitSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:payment_type", "bank,cash")
                .Annotation("Npgsql:Enum:receipt_position_state", "disbursed,planned,spent")
                .Annotation("Npgsql:Enum:role_change_request_resolution", "approved,dismissed");

            migrationBuilder.CreateTable(
                name: "AccountingUnits",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingUnits", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "RoleChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestComment = table.Column<string>(type: "text", nullable: true),
                    ResolvedBy = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Resolution = table.Column<RoleChangeRequest.ResolutionStatus>(type: "role_change_request_resolution", nullable: true),
                    ResolutionComment = table.Column<string>(type: "text", nullable: true),
                    RoleChanges = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleChangeRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiscalYears",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    AccountingUnitKey = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYears", x => new { x.AccountingUnitKey, x.Key });
                    table.ForeignKey(
                        name: "FK_FiscalYears_AccountingUnits_AccountingUnitKey",
                        column: x => x.AccountingUnitKey,
                        principalTable: "AccountingUnits",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Budgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountingUnitKey = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FiscalYearKey = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Budgets_AccountingUnits_AccountingUnitKey",
                        column: x => x.AccountingUnitKey,
                        principalTable: "AccountingUnits",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Budgets_FiscalYears_AccountingUnitKey_FiscalYearKey",
                        columns: x => new { x.AccountingUnitKey, x.FiscalYearKey },
                        principalTable: "FiscalYears",
                        principalColumns: new[] { "AccountingUnitKey", "Key" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Receipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReceiptAt = table.Column<DateOnly>(type: "date", nullable: false),
                    AccountingUnitKey = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FiscalYearKey = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receipts_AccountingUnits_AccountingUnitKey",
                        column: x => x.AccountingUnitKey,
                        principalTable: "AccountingUnits",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Receipts_FiscalYears_AccountingUnitKey_FiscalYearKey",
                        columns: x => new { x.AccountingUnitKey, x.FiscalYearKey },
                        principalTable: "FiscalYears",
                        principalColumns: new[] { "AccountingUnitKey", "Key" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_AccountingUnitKey_FiscalYearKey",
                table: "Budgets",
                columns: new[] { "AccountingUnitKey", "FiscalYearKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_AccountingUnitKey_FiscalYearKey",
                table: "Receipts",
                columns: new[] { "AccountingUnitKey", "FiscalYearKey" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleChangeRequests_RequestedBy",
                table: "RoleChangeRequests",
                column: "RequestedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Budgets");

            migrationBuilder.DropTable(
                name: "Receipts");

            migrationBuilder.DropTable(
                name: "RoleChangeRequests");

            migrationBuilder.DropTable(
                name: "FiscalYears");

            migrationBuilder.DropTable(
                name: "AccountingUnits");
        }
    }
}
