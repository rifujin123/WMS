using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                table: "StockAdjustments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "StockAdjustments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                table: "PurchaseOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_ApprovedById",
                table: "StockAdjustments",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ApprovedById",
                table: "PurchaseOrders",
                column: "ApprovedById");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_ApprovedById",
                table: "PurchaseOrders",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustments_AspNetUsers_ApprovedById",
                table: "StockAdjustments",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_ApprovedById",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustments_AspNetUsers_ApprovedById",
                table: "StockAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustments_ApprovedById",
                table: "StockAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_ApprovedById",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "PurchaseOrders");
        }
    }
}
