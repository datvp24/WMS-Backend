using Microsoft.EntityFrameworkCore;
using Wms.Api.Extensions;
using Wms.Infrastructure.Persistence.Context;
using Wms.Infrastructure.Seed;
using Wms.Application.Mapper.Sales;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURATION ---

// Cấu hình kết nối MySql
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddAuthServices();
builder.Services.AddControllers();
builder.Services.AddApplicationServices();
builder.Services.AddAutoMapper(typeof(SalesMappingProfile));
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddPermissionPolicies();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// --- 2. MIDDLEWARE PIPELINE ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// --- 3. DATABASE MIGRATION & SEEDING (CRITICAL SECTION) ---

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<AppDbContext>();

    // Retry logic lên đến 10 lần để chắc chắn DB đã lên
    int retryCount = 0;
    bool success = false;

    while (retryCount < 10 && !success)
    {
        try
        {
            logger.LogInformation(">>> Đang kiểm tra kết nối Database (Lần {Attempt})...", retryCount + 1);

            // 1. Thực thi Migration
            await context.Database.MigrateAsync();
            logger.LogInformation(">>> 1. Database Migration: THÀNH CÔNG.");

            // 2. Seed Auth Data
            logger.LogInformation(">>> 2. Đang nạp Auth Seed Data...");
            await AuthSeeder.SeedAsync(context);

            // 3. Seed Master Data
            logger.LogInformation(">>> 3. Đang nạp Master Seed Data...");
            await OtherSeeder.SeedAsync(context);

            logger.LogInformation(">>> TẤT CẢ DỮ LIỆU SEED ĐÃ ĐƯỢC XỬ LÝ.");
            success = true;
        }
        catch (Exception ex)
        {
            retryCount++;
            logger.LogWarning(">>> Database chưa sẵn sàng hoặc lỗi Seed: {Message}. Thử lại sau 5s...", ex.Message);
            await Task.Delay(5000); // Sử dụng Task.Delay thay vì Thread.Sleep

            if (retryCount >= 10)
            {
                logger.LogError(ex, ">>> LỖI NGHIÊM TRỌNG: Không thể khởi tạo Database sau nhiều lần thử.");
                throw;
            }
        }
    }
}

await app.RunAsync();