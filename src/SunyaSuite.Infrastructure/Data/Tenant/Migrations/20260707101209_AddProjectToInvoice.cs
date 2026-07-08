using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunyaSuite.Infrastructure.Data.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectRemark",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ProjectId",
                table: "Invoices",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Projects_ProjectId",
                table: "Invoices",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Projects_ProjectId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ProjectId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ProjectRemark",
                table: "Invoices");
        }
    }
}
