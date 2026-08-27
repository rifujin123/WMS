namespace WMS.Application.Configuration;

/// Cấu hình seed dữ liệu demo (section "Seed" trong appsettings / env).
public class DemoSeedOptions
{
    public const string SectionName = "Seed";

    /// Mặc định bật để demo; chỉ chạy trong Development (host guard). Tắt → seeder bỏ qua.
    public bool Enabled { get; init; } = true;

    /// Mật khẩu demo dùng chung cho các user seed (Chỉ dùng demo, không dùng production).
    public string DemoPassword { get; init; } = "Admin@123";
}