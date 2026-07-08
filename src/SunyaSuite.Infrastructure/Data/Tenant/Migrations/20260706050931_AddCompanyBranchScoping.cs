using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunyaSuite.Infrastructure.Data.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyBranchScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "Projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Projects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "NotificationPreferences",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "MoneyReceipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "MoneyReceipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Invites",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "FiscalYears",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "Clients",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Clients",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "AuditLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Projects_BranchId",
                table: "Projects",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CompanyId",
                table: "Projects",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_CompanyId",
                table: "NotificationPreferences",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyReceipts_BranchId",
                table: "MoneyReceipts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyReceipts_CompanyId",
                table: "MoneyReceipts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BranchId",
                table: "Invoices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CompanyId",
                table: "Invoices",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_CompanyId",
                table: "FiscalYears",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_BranchId",
                table: "Clients",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_CompanyId",
                table: "Clients",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CompanyId",
                table: "AuditLogs",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Companies_CompanyId",
                table: "AuditLogs",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Branches_BranchId",
                table: "Clients",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Companies_CompanyId",
                table: "Clients",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalYears_Companies_CompanyId",
                table: "FiscalYears",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Branches_BranchId",
                table: "Invoices",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Companies_CompanyId",
                table: "Invoices",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MoneyReceipts_Branches_BranchId",
                table: "MoneyReceipts",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MoneyReceipts_Companies_CompanyId",
                table: "MoneyReceipts",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationPreferences_Companies_CompanyId",
                table: "NotificationPreferences",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Branches_BranchId",
                table: "Projects",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Companies_CompanyId",
                table: "Projects",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Companies_CompanyId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Branches_BranchId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Companies_CompanyId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_FiscalYears_Companies_CompanyId",
                table: "FiscalYears");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Branches_BranchId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Companies_CompanyId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_MoneyReceipts_Branches_BranchId",
                table: "MoneyReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_MoneyReceipts_Companies_CompanyId",
                table: "MoneyReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationPreferences_Companies_CompanyId",
                table: "NotificationPreferences");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Branches_BranchId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Companies_CompanyId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_BranchId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_CompanyId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_NotificationPreferences_CompanyId",
                table: "NotificationPreferences");

            migrationBuilder.DropIndex(
                name: "IX_MoneyReceipts_BranchId",
                table: "MoneyReceipts");

            migrationBuilder.DropIndex(
                name: "IX_MoneyReceipts_CompanyId",
                table: "MoneyReceipts");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_BranchId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CompanyId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_FiscalYears_CompanyId",
                table: "FiscalYears");

            migrationBuilder.DropIndex(
                name: "IX_Clients_BranchId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_CompanyId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_CompanyId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "MoneyReceipts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "MoneyReceipts");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Invites");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "FiscalYears");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AuditLogs");
        }
    }
}
