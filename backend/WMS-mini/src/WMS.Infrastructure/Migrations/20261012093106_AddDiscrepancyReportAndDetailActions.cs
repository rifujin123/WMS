using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscrepancyReportAndDetailActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscrepancyAction",
                table: "ReceivingDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscrepancyNotes",
                table: "ReceivingDetails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiscrepancyReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VendorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EvidenceImagesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_DiscrepancyReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscrepancyReports_AspNetUsers_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DiscrepancyReports_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscrepancyReports_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscrepancyReports_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscrepancyReports_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscrepancyReports_Receivings_ReceivingId",
                        column: x => x.ReceivingId,
                        principalTable: "Receivings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscrepancyReports_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscrepancyReports_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscrepancyReports_ApprovedById",
                table: "DiscrepancyReports",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_DiscrepancyReports_CreatedById",
                table: "DiscrepancyReports",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DiscrepancyReports_DeletedById",
                table: "DiscrepancyReports",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_DiscrepancyReports_PurchaseOrderId",
                table: "DiscrepancyReports",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscrepancyReports_ReceivingId",
                table: "DiscrepancyReports",
                column: "ReceivingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscrepancyReports_TenantId_ReceivingId",
                table: "DiscrepancyReports",
                columns: new[] { "TenantId", "ReceivingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscrepancyReports_UpdatedById",
                table: "DiscrepancyReports",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DiscrepancyReports_VendorId",
                table: "DiscrepancyReports",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscrepancyReports");

            migrationBuilder.DropColumn(
                name: "DiscrepancyAction",
                table: "ReceivingDetails");

            migrationBuilder.DropColumn(
                name: "DiscrepancyNotes",
                table: "ReceivingDetails");
        }
    }
}
