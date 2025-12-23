using Microsoft.EntityFrameworkCore;
using Wms.Api.Extensions;
using Wms.Infrastructure.Persistence.Context;
using Wms.Infrastructure.Seed;
using Wms.Application.Mapper.Sales;
using AutoMapper;
var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURATION ---

// Add DbContext (SQL Server) - FIX FOR AGGREGATE EXCEPTION
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

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

// 3. Run Auth Seeders (Role, Permission, Admin)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();      // Auto migrate DB on start
    await AuthSeeder.SeedAsync(db);        // Seed admin + roles + permissions
}

app.Run();