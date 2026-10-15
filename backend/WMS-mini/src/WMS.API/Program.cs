using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
﻿using System.Text;
using System.Text.Json.Serialization;
using CloudinaryDotNet;
using System.Net.Http.Headers;
using WMS.API.Configuration;
using WMS.API.Middlewares;
using WMS.Application.Configuration;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Repositories;
using WMS.Infrastructure.Services;
using WMS.Infrastructure.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using WMS.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// Táº¯t reload-on-change cho cÃ¡c JSON configuration source.
// LÃ½ do: trong container Linux, má»—i file JSON táº¡o má»™t FileSystemWatcher. Khi image
// chá»©a toÃ n bá»™ source code, tá»•ng sá»‘ watcher vÆ°á»£t quÃ¡ giá»›i háº¡n inotify (máº·c Ä‘á»‹nh 128/user),
// gÃ¢y lá»—i "inotify instances limit reached" lÃºc khá»Ÿi Ä‘á»™ng.
// Production dÃ¹ng biáº¿n mÃ´i trÆ°á»ng Ä‘á»ƒ cáº¥u hÃ¬nh, khÃ´ng cáº§n reload file runtime.
foreach (var src in builder.Configuration.Sources
    .OfType<Microsoft.Extensions.Configuration.Json.JsonConfigurationSource>()
    .ToList())
{
    src.ReloadOnChange = false;
}

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.Converters.Add(new DateTimeUtcJsonConverter());
        o.JsonSerializerOptions.Converters.Add(new NullableDateTimeUtcJsonConverter());
    });

builder.Services.AddOptions<PaginationOptions>()
    .Bind(builder.Configuration.GetSection(PaginationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AiProviderOptions>()
    .Bind(builder.Configuration.GetSection(AiProviderOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<DemoSeedOptions>()
    .Bind(builder.Configuration.GetSection(DemoSeedOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient("AiProvider", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<AiProviderOptions>>().Value;
    var serviceUrl = string.IsNullOrWhiteSpace(options.ServiceUrl)
        ? "http://localhost:8000"
        : options.ServiceUrl;
    client.BaseAddress = new Uri(serviceUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 60);
});

var frontendOrigin = builder.Configuration["Frontend:Origin"]
    ?? "http://localhost:5173";

builder.Services.AddCors(o =>
{
    o.AddPolicy("Frontend", p =>
        p.WithOrigins(frontendOrigin)
         .AllowAnyHeader()
         .AllowAnyMethod());
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync("{\"success\":false,\"code\":429,\"message\":\"Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau 1 phút.\",\"data\":null}", token);
    };

    // Toàn hệ thống (Global): 100 req / phút / IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    // Riêng Auth (Login/Register): 5 req / phút / IP
    options.AddPolicy("AuthPolicy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "WMS API",
        Version = "v1"
    });

    var jwtScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter your JWT token (no need to include 'Bearer ' â€” Swagger will add it automatically).",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
            Id = "Bearer",
            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { jwtScheme, Array.Empty<string>() }
    });
});

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<WMS.Application.Mappings.MappingProfile>();
});
builder.Services.AddDbContext<WmsDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<User>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<WmsDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured. Set it in appsettings.Development.json or environment variable Jwt__Key.");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Cloudinary (dÃ¹ng chung 1 instance, thread-safe)
var cloudName = builder.Configuration["Cloudinary:CloudName"];
if (string.IsNullOrWhiteSpace(cloudName))
    throw new InvalidOperationException("Cloudinary:CloudName is not configured.");
var cloudApiKey = builder.Configuration["Cloudinary:ApiKey"];
if (string.IsNullOrWhiteSpace(cloudApiKey))
    throw new InvalidOperationException("Cloudinary:ApiKey is not configured.");
var cloudApiSecret = builder.Configuration["Cloudinary:ApiSecret"];
if (string.IsNullOrWhiteSpace(cloudApiSecret))
    throw new InvalidOperationException("Cloudinary:ApiSecret is not configured.");
var cloudinary = new Cloudinary(new Account(cloudName, cloudApiKey, cloudApiSecret));
cloudinary.Api.Secure = true; // tráº£ vá» https:// URL
builder.Services.AddSingleton(cloudinary);

// Services
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IVendorDebtService, VendorDebtService>();
builder.Services.AddScoped<IBackorderLinkService, BackorderLinkService>();
builder.Services.AddScoped<IPutAwayService, PutAwayService>();
builder.Services.AddScoped<IReceivingService, ReceivingService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IStockAdjustmentService, StockAdjustmentService>();
builder.Services.AddScoped<IStockTransferService, StockTransferService>();
builder.Services.AddScoped<ISaleOrderService, SaleOrderService>();
builder.Services.AddScoped<IPickingService, PickingService>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<IShipmentGateway, DemoShipmentGateway>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IStockMovementService, StockMovementService>();
builder.Services.AddScoped<IWorkingCapitalRadarService, WorkingCapitalRadarService>();
builder.Services.AddScoped<IFcmPushService, FcmPushService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDemoDataSeeder, MultiStatusDemoDataSeeder>();

// AI Provider
builder.Services.AddScoped<IAiProvider, AiProvider>();
builder.Services.AddScoped<IProductMappingService, ProductMappingService>();
builder.Services.AddScoped<IInvoiceScanService, InvoiceScanService>();

// Repositories
builder.Services.AddScoped<IProductRepository, SqlProductRepository>();
builder.Services.AddScoped<ICategoryRepository, SqlCategoryRepository>();
builder.Services.AddScoped<ICustomerRepository, SqlCustomerRepository>();
builder.Services.AddScoped<IVendorRepository, SqlVendorRepository>();
builder.Services.AddScoped<IVendorDebtRepository, SqlVendorDebtRepository>();
builder.Services.AddScoped<IWarehouseRepository, SqlWarehouseRepository>();
builder.Services.AddScoped<ILocationRepository, SqlLocationRepository>();
builder.Services.AddScoped<IPurchaseOrderRepository, SqlPurchaseOrderRepository>();
builder.Services.AddScoped<IStockRepository, SqlStockRepository>();
builder.Services.AddScoped<IStockMovementRepository, SqlStockMovementRepository>();
builder.Services.AddScoped<IBackorderLinkRepository, SqlBackorderLinkRepository>();
builder.Services.AddScoped<ISaleOrderRepository, SqlSaleOrderRepository>();
builder.Services.AddScoped<IPickingRepository, SqlPickingRepository>();
builder.Services.AddScoped<IReceivingRepository, SqlReceivingRepository>();
builder.Services.AddScoped<IPutAwayTaskRepository, SqlPutAwayTaskRepository>();
builder.Services.AddScoped<IShipmentRepository, SqlShipmentRepository>();
builder.Services.AddScoped<IRmaRepository, SqlRmaRepository>();
builder.Services.AddScoped<IAssociationRuleRepository, SqlAssociationRuleRepository>();
builder.Services.AddScoped<IStockAdjustmentRepository, SqlStockAdjustmentRepository>();
builder.Services.AddScoped<IAuditLogRepository, SqlAuditLogRepository>();

builder.Services.AddHostedService<ReservationTimeoutWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
    db.Database.Migrate();
}

// Seed roles and default admin
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var role in new[] { "Admin", "WarehouseManager", "WarehouseStaff", "ChiefAccountant", "Accountant" })
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    const string adminUsername = "admin";
    // Äá»c máº­t kháº©u seed tá»« cáº¥u hÃ¬nh (env SEED__ADMIN_PASSWORD); mÃ´i trÆ°á»ng dev dÃ¹ng default an toÃ n.
    // KhÃ´ng hardcode password trong source; production nÃªn set SEED__ADMIN_PASSWORD.
    var adminPassword = builder.Configuration["Seed:AdminPassword"]
        ?? "Admin@123";
    if (await userManager.FindByNameAsync(adminUsername) == null)
    {
        var adminUser = new User
        {
            UserName  = adminUsername,
            Email     = "admin@wms.local",
            FullName  = "System Administrator",
            CreatedAt = DateTime.UtcNow
        };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

// Seed dá»¯ liá»‡u demo (chá»‰ cháº¡y trong Development; báº£n thÃ¢n seeder kiá»ƒm tra Seed:Enabled)
if (app.Environment.IsDevelopment())
{
    using var demoSeedScope = app.Services.CreateScope();
    var demoSeeder = demoSeedScope.ServiceProvider.GetRequiredService<IDemoDataSeeder>();
    await demoSeeder.SeedAsync();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseRateLimiter();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
