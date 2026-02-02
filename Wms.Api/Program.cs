using AutoMapper;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Wms.Api.Extensions;
using Wms.Application.Exceptions;
using Wms.Application.Mapper.Sales;
using Wms.Infrastructure.Persistence.Context;
using Wms.Infrastructure.Seed;
var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURATION ---

// Add DbContext (SQL Server) - FIX FOR AGGREGATE EXCEPTION
// --- 1. CONFIGURATION ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 30)); // Cố định phiên bản để tránh AutoDetect lỗi

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion, mysqlOptions =>
        mysqlOptions.EnableRetryOnFailure())); // Thêm cái này để Docker chạy ổn định hơn
// Add Services (AuthService, JwtService, PasswordHasher)
builder.Services.AddAuthServices();

// Add Core Services
builder.Services.AddControllers();

// Add Application Services (Gom nhóm các AddScoped lại)
builder.Services.AddApplicationServices();

builder.Services.AddAutoMapper(typeof(SalesMappingProfile));

// Add JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add Authorization (optional policy setup)
builder.Services.AddPermissionPolicies();
builder.Services.AddHttpContextAccessor(); // Chỉ giữ lại một lần gọi

// Add Controllers + Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(Program));

// Add CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


// --- 2. BUILD APP AND MIDDLEWARE PIPELINE ---

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
//app.UseExceptionHandler(builder =>
//{
//    builder.Run(async context =>
//    {
//        var feature = context.Features.Get<IExceptionHandlerFeature>();
//        var error = feature?.Error;

//        context.Response.ContentType = "application/json";

//        // ✅ Lỗi nghiệp vụ
//        if (error is BusinessException be)
//        {
//            context.Response.StatusCode = StatusCodes.Status400BadRequest;
//            await context.Response.WriteAsJsonAsync(new
//            {
//                code = be.Code,
//                message = be.Message
//            });
//            return;
//        }

//        // ✅ Lỗi hệ thống (fallback)
//        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

//        var env = context.RequestServices.GetRequiredService<IHostEnvironment>();

//        if (env.IsDevelopment())
//        {
//            // DEV: trả lỗi gốc để debug
//            await context.Response.WriteAsJsonAsync(new
//            {
//                code = "SYSTEM_ERROR",
//                message = error?.Message,
//                stackTrace = error?.StackTrace
//            });
//        }
//        else
//        {
//            // PROD: che chi tiết
//            await context.Response.WriteAsJsonAsync(new
//            {
//                code = "SYSTEM_ERROR",
//                message = "Có lỗi hệ thống xảy ra. Vui lòng thử lại sau."
//            });
//        }
//    });
//});


// 3. Run Auth Seeders (Role, Permission, Admin)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    int retry = 0;
    while (retry < 5)
    {
        try
        {
            await db.Database.MigrateAsync();
            await AuthSeeder.SeedAsync(db);
            await TechnicalPlasticWarehouseSeeder.SeedAsync(db);
            break; // Thành công thì thoát vòng lặp
        }
        catch (Exception ex)
        {
            retry++;
            logger.LogWarning("DB chưa sẵn sàng, đang thử lại lần {0}...", retry);
            await Task.Delay(5000); // Đợi 5s rồi thử lại
        }
    }
}
await app.RunAsync();
