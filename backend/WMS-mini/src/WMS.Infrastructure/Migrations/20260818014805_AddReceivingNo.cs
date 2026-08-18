using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceivingNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceivingNo",
                table: "Receivings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Backfill mã phiếu nhận cho dữ liệu cũ — tránh trùng '' khi tạo unique index
            migrationBuilder.Sql(
                "UPDATE [Receivings] SET [ReceivingNo] = 'RC-' + REPLACE(CONVERT(varchar(36), [Id]), '-', '') WHERE [ReceivingNo] = ''");

            migrationBuilder.CreateIndex(
                name: "IX_Receivings_ReceivingNo",
                table: "Receivings",
                column: "ReceivingNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receivings_ReceivingNo",
                table: "Receivings");

            migrationBuilder.DropColumn(
                name: "ReceivingNo",
                table: "Receivings");
        }
    }
}
