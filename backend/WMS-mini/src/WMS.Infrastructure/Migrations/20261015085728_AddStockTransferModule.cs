using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransferModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FromWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ShippedDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransfers_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransfers_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransfers_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransfers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransfers_Warehouses_FromWarehouseId",
                        column: x => x.FromWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransfers_Warehouses_ToWarehouseId",
                        column: x => x.ToWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockTransferDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockTransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransferQty = table.Column<int>(type: "int", nullable: false),
                    ReceivedQty = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_StockTransferDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferDetails_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransferDetails_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransferDetails_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransferDetails_Locations_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransferDetails_Locations_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransferDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransferDetails_StockTransfers_StockTransferId",
                        column: x => x.StockTransferId,
                        principalTable: "StockTransfers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransferDetails_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferDetails_CreatedById",
                table: "StockTransferDetails",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferDetails_DeletedById",
                table: "StockTransferDetails",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferDetails_FromLocationId",
                table: "StockTransferDetails",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferDetails_ProductId",
                table: "StockTransferDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferDetails_StockTransferId",
                table: "StockTransferDetails",
                column: "StockTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferDetails_TenantId",
                table: "StockTransferDetails",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferDetails_ToLocationId",
                table: "StockTransferDetails",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferDetails_UpdatedById",
                table: "StockTransferDetails",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_CreatedById",
                table: "StockTransfers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_DeletedById",
                table: "StockTransfers",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_FromWarehouseId",
                table: "StockTransfers",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_TenantId",
                table: "StockTransfers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ToWarehouseId",
                table: "StockTransfers",
                column: "ToWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_UpdatedById",
                table: "StockTransfers",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockTransferDetails");

            migrationBuilder.DropTable(
                name: "StockTransfers");
        }
    }
}
