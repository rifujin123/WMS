using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreOrderAndCustomerLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "SaleOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DepositDeadline",
                table: "SaleOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepositNotes",
                table: "SaleOrders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPreOrder",
                table: "SaleOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidDeposit",
                table: "SaleOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RequiredDeposit",
                table: "SaleOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "SaleOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "SaleOrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrders_CustomerId",
                table: "SaleOrders",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleOrders_Customers_CustomerId",
                table: "SaleOrders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleOrders_Customers_CustomerId",
                table: "SaleOrders");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrders_CustomerId",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "DepositDeadline",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "DepositNotes",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "IsPreOrder",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "PaidDeposit",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "RequiredDeposit",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "SaleOrderDetails");
        }
    }
}
