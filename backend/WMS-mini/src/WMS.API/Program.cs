using System.Text;
using WMS.API.Middlewares;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Repositories;
using WMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using WMS.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
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
        Description = "Enter your JWT token (no need to include 'Bearer ' — Swagger will add it automatically).",
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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IReceivingService, ReceivingService>();

// Repositories
builder.Services.AddScoped<IProductRepository, SqlProductRepository>();
builder.Services.AddScoped<ICategoryRepository, SqlCategoryRepository>();
builder.Services.AddScoped<IWarehouseRepository, SqlWarehouseRepository>();
builder.Services.AddScoped<ILocationRepository, SqlLocationRepository>();
builder.Services.AddScoped<IPurchaseOrderRepository, SqlPurchaseOrderRepository>();
builder.Services.AddScoped<IStockRepository, SqlStockRepository>();
builder.Services.AddScoped<IStockMovementRepository, SqlStockMovementRepository>();
builder.Services.AddScoped<ISaleOrderRepository, SqlSaleOrderRepository>();
builder.Services.AddScoped<IPickingRepository, SqlPickingRepository>();
builder.Services.AddScoped<IReceivingRepository, SqlReceivingRepository>();
builder.Services.AddScoped<IPutAwayTaskRepository, SqlPutAwayTaskRepository>();
builder.Services.AddScoped<IShipmentRepository, SqlShipmentRepository>();
builder.Services.AddScoped<IRmaRepository, SqlRmaRepository>();
builder.Services.AddScoped<IAssociationRuleRepository, SqlAssociationRuleRepository>();

var app = builder.Build();

// Seed roles and default admin
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var role in new[] { "Admin", "WarehouseManager", "WarehouseStaff" })
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    const string adminUsername = "admin";
    const string adminPassword = "Admin@123";
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

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
