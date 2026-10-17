using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Models;

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
                .Annotation("Npgsql:Enum:payment_type", "card,cash")
                .Annotation("Npgsql:Enum:receipt_position_state", "disbursed,planned,spent")
                .Annotation("Npgsql:Enum:receipt_type", "booking,budget_installment,carry_over");

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
                name: "FiscalYears",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AccountingUnitKey = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYears", x => x.Key);
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
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    FiscalYearKey = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Budgets_FiscalYears_FiscalYearKey",
                        column: x => x.FiscalYearKey,
                        principalTable: "FiscalYears",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Receipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceiptType = table.Column<Receipt.Type>(type: "receipt_type", nullable: false),
                    PaymentType = table.Column<PaymentType>(type: "payment_type", nullable: false),
                    Signature1 = table.Column<string>(type: "text", nullable: true),
                    Signature2 = table.Column<string>(type: "text", nullable: true),
                    FiscalYearKey = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receipts_FiscalYears_FiscalYearKey",
                        column: x => x.FiscalYearKey,
                        principalTable: "FiscalYears",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    BudgetId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetAttachments_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BudgetGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    BudgetId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetGroups_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    ReceiptId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptAttachments_Receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "Receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    State = table.Column<ReceiptPositionState>(type: "receipt_position_state", nullable: false),
                    PaidBy = table.Column<string>(type: "text", nullable: true),
                    ReceiptId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptPositions_Receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "Receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BudgetPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlannedExpenses = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PlannedIncome = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DeficitBudgetPositionId = table.Column<int>(type: "integer", nullable: true),
                    BudgetGroupId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetPositions_BudgetGroups_BudgetGroupId",
                        column: x => x.BudgetGroupId,
                        principalTable: "BudgetGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetPositions_BudgetPositions_DeficitBudgetPositionId",
                        column: x => x.DeficitBudgetPositionId,
                        principalTable: "BudgetPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptPositionAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    ReceiptPositionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptPositionAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptPositionAttachments_ReceiptPositions_ReceiptPosition~",
                        column: x => x.ReceiptPositionId,
                        principalTable: "ReceiptPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetAttachments_BudgetId",
                table: "BudgetAttachments",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetGroups_BudgetId",
                table: "BudgetGroups",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPositions_BudgetGroupId",
                table: "BudgetPositions",
                column: "BudgetGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPositions_DeficitBudgetPositionId",
                table: "BudgetPositions",
                column: "DeficitBudgetPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_FiscalYearKey",
                table: "Budgets",
                column: "FiscalYearKey");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_AccountingUnitKey",
                table: "FiscalYears",
                column: "AccountingUnitKey");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAttachments_ReceiptId",
                table: "ReceiptAttachments",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptPositionAttachments_ReceiptPositionId",
                table: "ReceiptPositionAttachments",
                column: "ReceiptPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptPositions_ReceiptId",
                table: "ReceiptPositions",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_FiscalYearKey_Key",
                table: "Receipts",
                columns: new[] { "FiscalYearKey", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetAttachments");

            migrationBuilder.DropTable(
                name: "BudgetPositions");

            migrationBuilder.DropTable(
                name: "ReceiptAttachments");

            migrationBuilder.DropTable(
                name: "ReceiptPositionAttachments");

            migrationBuilder.DropTable(
                name: "BudgetGroups");

            migrationBuilder.DropTable(
                name: "ReceiptPositions");

            migrationBuilder.DropTable(
                name: "Budgets");

            migrationBuilder.DropTable(
                name: "Receipts");

            migrationBuilder.DropTable(
                name: "FiscalYears");

            migrationBuilder.DropTable(
                name: "AccountingUnits");
        }
    }
}
