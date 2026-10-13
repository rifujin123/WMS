using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorDebtLedgerAndPoCreditOffset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CreditOffsetAmount",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "VendorId",
                table: "PurchaseOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VendorDebtLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VendorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SourceReceivingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DebtType = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedInPoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_VendorDebtLedgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorDebtLedgers_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorDebtLedgers_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorDebtLedgers_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorDebtLedgers_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendorDebtLedgers_PurchaseOrders_ResolvedInPoId",
                        column: x => x.ResolvedInPoId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendorDebtLedgers_Receivings_SourceReceivingId",
                        column: x => x.SourceReceivingId,
                        principalTable: "Receivings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorDebtLedgers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorDebtLedgers_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_VendorId",
                table: "PurchaseOrders",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebtLedgers_CreatedById",
                table: "VendorDebtLedgers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebtLedgers_DeletedById",
                table: "VendorDebtLedgers",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebtLedgers_ProductId",
                table: "VendorDebtLedgers",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebtLedgers_ResolvedInPoId",
                table: "VendorDebtLedgers",
                column: "ResolvedInPoId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebtLedgers_SourceReceivingId",
                table: "VendorDebtLedgers",
                column: "SourceReceivingId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebtLedgers_TenantId",
                table: "VendorDebtLedgers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebtLedgers_UpdatedById",
                table: "VendorDebtLedgers",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebtLedgers_VendorId",
                table: "VendorDebtLedgers",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Vendors_VendorId",
                table: "PurchaseOrders",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Vendors_VendorId",
                table: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "VendorDebtLedgers");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_VendorId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CreditOffsetAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "PurchaseOrders");
        }
    }
}
