using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunyaSuite.Infrastructure.Data.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectIdToInvoiceItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "InvoiceItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_ProjectId",
                table: "InvoiceItems",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_Projects_ProjectId",
                table: "InvoiceItems",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_Projects_ProjectId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_ProjectId",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "InvoiceItems");
        }
    }
}
