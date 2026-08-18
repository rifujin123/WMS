using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceOneConfirmedReceivingPerPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receivings_PurchaseOrderId",
                table: "Receivings");

            migrationBuilder.CreateIndex(
                name: "IX_Receivings_PurchaseOrderId",
                table: "Receivings",
                column: "PurchaseOrderId",
                unique: true,
                filter: "[Status] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receivings_PurchaseOrderId",
                table: "Receivings");

            migrationBuilder.CreateIndex(
                name: "IX_Receivings_PurchaseOrderId",
                table: "Receivings",
                column: "PurchaseOrderId");
        }
    }
}
