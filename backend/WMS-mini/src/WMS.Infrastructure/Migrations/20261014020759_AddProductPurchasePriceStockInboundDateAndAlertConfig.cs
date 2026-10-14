using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPurchasePriceStockInboundDateAndAlertConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeadStockDays",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.AddColumn<bool>(
                name: "EnableExpiryAlertBanner",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "FrozenCapitalDays",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<int>(
                name: "NearExpiryDays",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<DateTime>(
                name: "InboundDate",
                table: "Stocks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchasePrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeadStockDays",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "EnableExpiryAlertBanner",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FrozenCapitalDays",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "NearExpiryDays",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "InboundDate",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "PurchasePrice",
                table: "Products");
        }
    }
}
