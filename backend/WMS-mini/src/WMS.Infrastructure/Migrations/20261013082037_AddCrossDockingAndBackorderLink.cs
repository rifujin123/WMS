using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCrossDockingAndBackorderLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCrossDock",
                table: "PutAwayTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PutAwayTasks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SaleOrderId",
                table: "PutAwayTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BackorderLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleOrderDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    FulfilledQuantity = table.Column<int>(type: "int", nullable: false),
                    IsFulfilled = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackorderLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackorderLinks_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BackorderLinks_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BackorderLinks_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BackorderLinks_PurchaseOrderDetails_PurchaseOrderDetailId",
                        column: x => x.PurchaseOrderDetailId,
                        principalTable: "PurchaseOrderDetails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BackorderLinks_SaleOrderDetails_SaleOrderDetailId",
                        column: x => x.SaleOrderDetailId,
                        principalTable: "SaleOrderDetails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BackorderLinks_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PutAwayTasks_SaleOrderId",
                table: "PutAwayTasks",
                column: "SaleOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_BackorderLinks_CreatedById",
                table: "BackorderLinks",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BackorderLinks_DeletedById",
                table: "BackorderLinks",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_BackorderLinks_PurchaseOrderDetailId",
                table: "BackorderLinks",
                column: "PurchaseOrderDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_BackorderLinks_SaleOrderDetailId",
                table: "BackorderLinks",
                column: "SaleOrderDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_BackorderLinks_TenantId_PurchaseOrderDetailId",
                table: "BackorderLinks",
                columns: new[] { "TenantId", "PurchaseOrderDetailId" });

            migrationBuilder.CreateIndex(
                name: "IX_BackorderLinks_TenantId_SaleOrderDetailId",
                table: "BackorderLinks",
                columns: new[] { "TenantId", "SaleOrderDetailId" });

            migrationBuilder.CreateIndex(
                name: "IX_BackorderLinks_UpdatedById",
                table: "BackorderLinks",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_PutAwayTasks_SaleOrders_SaleOrderId",
                table: "PutAwayTasks",
                column: "SaleOrderId",
                principalTable: "SaleOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PutAwayTasks_SaleOrders_SaleOrderId",
                table: "PutAwayTasks");

            migrationBuilder.DropTable(
                name: "BackorderLinks");

            migrationBuilder.DropIndex(
                name: "IX_PutAwayTasks_SaleOrderId",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "IsCrossDock",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "SaleOrderId",
                table: "PutAwayTasks");
        }
    }
}
