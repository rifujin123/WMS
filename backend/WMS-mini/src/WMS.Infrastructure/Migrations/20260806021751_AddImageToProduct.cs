using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImageToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: ImageUrl (Products) và AvatarUrl (AspNetUsers) đã tồn tại trong DB
            // từ các lần ALTER TABLE thủ công trước đó. Migration này chỉ để ghi nhận
            // vào __EFMigrationsHistory cho khớp với model.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
