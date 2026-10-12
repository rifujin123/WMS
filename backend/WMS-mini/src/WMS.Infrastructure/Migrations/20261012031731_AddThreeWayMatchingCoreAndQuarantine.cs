using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddThreeWayMatchingCoreAndQuarantine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receivings_TenantId_PurchaseOrderId",
                table: "Receivings");

            migrationBuilder.AddColumn<string>(
                name: "DiscrepancyReason",
                table: "Receivings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasDiscrepancy",
                table: "Receivings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceDate",
                table: "Receivings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "Receivings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InvoiceTotalAmount",
                table: "Receivings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "ReceivingDetails",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "PurchaseOrderDetails",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "PurchaseOrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscrepancyReason",
                table: "Receivings");

            migrationBuilder.DropColumn(
                name: "HasDiscrepancy",
                table: "Receivings");

            migrationBuilder.DropColumn(
                name: "InvoiceDate",
                table: "Receivings");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "Receivings");

            migrationBuilder.DropColumn(
                name: "InvoiceTotalAmount",
                table: "Receivings");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "ReceivingDetails");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "PurchaseOrderDetails");

            migrationBuilder.CreateIndex(
                name: "IX_Receivings_TenantId_PurchaseOrderId",
                table: "Receivings",
                columns: new[] { "TenantId", "PurchaseOrderId" },
                unique: true,
                filter: "[Status] = 1");
        }
    }
}
