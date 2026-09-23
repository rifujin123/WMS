using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_AspNetUsers_CreatedById",
                table: "Warehouses");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_AspNetUsers_DeletedById",
                table: "Warehouses");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_AspNetUsers_UpdatedById",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_CreatedById",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_DeletedById",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_UpdatedById",
                table: "Warehouses");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById1",
                table: "Warehouses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById1",
                table: "Warehouses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById1",
                table: "Warehouses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_CreatedById1",
                table: "Warehouses",
                column: "CreatedById1");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_DeletedById1",
                table: "Warehouses",
                column: "DeletedById1");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_UpdatedById1",
                table: "Warehouses",
                column: "UpdatedById1");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_WarehouseId",
                table: "AspNetUsers",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Warehouses_WarehouseId",
                table: "AspNetUsers",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_AspNetUsers_CreatedById1",
                table: "Warehouses",
                column: "CreatedById1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_AspNetUsers_DeletedById1",
                table: "Warehouses",
                column: "DeletedById1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_AspNetUsers_UpdatedById1",
                table: "Warehouses",
                column: "UpdatedById1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Warehouses_WarehouseId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_AspNetUsers_CreatedById1",
                table: "Warehouses");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_AspNetUsers_DeletedById1",
                table: "Warehouses");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_AspNetUsers_UpdatedById1",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_CreatedById1",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_DeletedById1",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_UpdatedById1",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_WarehouseId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedById1",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "DeletedById1",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "UpdatedById1",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_CreatedById",
                table: "Warehouses",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_DeletedById",
                table: "Warehouses",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_UpdatedById",
                table: "Warehouses",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_AspNetUsers_CreatedById",
                table: "Warehouses",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_AspNetUsers_DeletedById",
                table: "Warehouses",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_AspNetUsers_UpdatedById",
                table: "Warehouses",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
