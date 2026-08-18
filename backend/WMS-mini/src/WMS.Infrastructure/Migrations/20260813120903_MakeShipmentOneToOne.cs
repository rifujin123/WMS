using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeShipmentOneToOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_SaleOrderId",
                table: "Shipments");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_SaleOrderId",
                table: "Shipments",
                column: "SaleOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_SaleOrderId",
                table: "Shipments");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_SaleOrderId",
                table: "Shipments",
                column: "SaleOrderId");
        }
    }
}
