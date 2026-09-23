using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseToPurchaseOrderAndSaleOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "SaleOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "PurchaseOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrders_WarehouseId",
                table: "SaleOrders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_WarehouseId",
                table: "PurchaseOrders",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Warehouses_WarehouseId",
                table: "PurchaseOrders",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleOrders_Warehouses_WarehouseId",
                table: "SaleOrders",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Warehouses_WarehouseId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleOrders_Warehouses_WarehouseId",
                table: "SaleOrders");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrders_WarehouseId",
                table: "SaleOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_WarehouseId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "PurchaseOrders");
        }
    }
}
