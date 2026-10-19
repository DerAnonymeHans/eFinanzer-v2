using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using eFinanzer.Common.Database.Models;

#nullable disable

namespace eFinanzer.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleChangeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:payment_type", "bank,cash")
                .Annotation("Npgsql:Enum:receipt_position_state", "disbursed,planned,spent")
                .Annotation("Npgsql:Enum:receipt_type", "booking,budget_installment,carry_over")
                .Annotation("Npgsql:Enum:role_change_request_resolution", "approved,dismissed")
                .OldAnnotation("Npgsql:Enum:payment_type", "bank,cash")
                .OldAnnotation("Npgsql:Enum:receipt_position_state", "disbursed,planned,spent")
                .OldAnnotation("Npgsql:Enum:receipt_type", "booking,budget_installment,carry_over");

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

            migrationBuilder.CreateIndex(
                name: "IX_RoleChangeRequests_RequestedBy",
                table: "RoleChangeRequests",
                column: "RequestedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleChangeRequests");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:payment_type", "bank,cash")
                .Annotation("Npgsql:Enum:receipt_position_state", "disbursed,planned,spent")
                .Annotation("Npgsql:Enum:receipt_type", "booking,budget_installment,carry_over")
                .OldAnnotation("Npgsql:Enum:payment_type", "bank,cash")
                .OldAnnotation("Npgsql:Enum:receipt_position_state", "disbursed,planned,spent")
                .OldAnnotation("Npgsql:Enum:receipt_type", "booking,budget_installment,carry_over")
                .OldAnnotation("Npgsql:Enum:role_change_request_resolution", "approved,dismissed");
        }
    }
}
