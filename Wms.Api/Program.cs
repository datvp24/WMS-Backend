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
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 30));
Console.WriteLine($"==> CONNECTION STRING: {connectionString}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion, mysqlOptions =>
    {
        mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));

// Add Services (AuthService, JwtService, PasswordHasher)
builder.Services.AddAuthServices();

// Add Core Services
builder.Services.AddControllers();

// Add Application Services
builder.Services.AddApplicationServices();

builder.Services.AddAutoMapper(typeof(SalesMappingProfile));

// Add JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add Authorization
builder.Services.AddPermissionPolicies();
builder.Services.AddHttpContextAccessor();

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

// --- 3. Run Auth Seeders ---
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
            break;
        }
        catch (Exception ex)
        {
            retry++;
            logger.LogWarning("DB chưa sẵn sàng, đang thử lại lần {0}... Lỗi: {1}", retry, ex.Message);
            await Task.Delay(5000);
        }
    }
}

await app.RunAsync();