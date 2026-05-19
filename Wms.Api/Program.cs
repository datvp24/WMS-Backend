using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using Wms.Api.Extensions;
using Wms.Application.Mapper.Sales;
using Wms.Infrastructure.Persistence.Context;
using Wms.Infrastructure.Seed;
using Wms.Infrastructure.WebSockets;

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

// ✅ Đăng ký WebSocket hub + background worker
builder.Services.AddSingleton<InventoryWebSocketHub>();
builder.Services.AddHostedService<InventoryBroadcastWorker>();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// --- 2. BUILD APP ---
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// ✅ UseWebSockets phải đặt TRƯỚC app.Map("/ws/...")
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ✅ WS endpoint đầy đủ — giữ connection alive và unregister khi đóng
app.Map("/ws/inventory", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var hub = context.RequestServices.GetRequiredService<InventoryWebSocketHub>();
    var ws = await context.WebSockets.AcceptWebSocketAsync();
    var id = hub.Register(ws);

    var logger = context.RequestServices
        .GetRequiredService<ILogger<Program>>();
    logger.LogInformation("WS client connected: {Id}", id);

    // ── Giữ connection sống cho đến khi client đóng ──
    var buffer = new byte[64];
    try
    {
        var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
        while (!result.CloseStatus.HasValue)
        {
            // Client không cần gửi gì — chỉ cần giữ vòng lặp
            result = await ws.ReceiveAsync(buffer, CancellationToken.None);
        }

        await ws.CloseAsync(
            result.CloseStatus.Value,
            result.CloseStatusDescription,
            CancellationToken.None);
    }
    catch
    {
        // Disconnect đột ngột — bỏ qua
    }
    finally
    {
        hub.Unregister(id);
        logger.LogInformation("WS client disconnected: {Id}", id);
    }
});

// --- 3. Migrate + Seed ---
_ = Task.Run(async () =>
{
    await Task.Delay(3000);
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    int retry = 0;
    while (retry < 10)
    {
        try
        {
            await db.Database.MigrateAsync();
            await AuthSeeder.SeedAsync(db);
            await TechnicalPlasticWarehouseSeeder.SeedAsync(db);
            logger.LogInformation("✅ DB migration và seed thành công!");
            break;
        }
        catch (Exception ex)
        {
            retry++;
            logger.LogWarning("⚠️ DB chưa sẵn sàng, thử lại {0}/10: {1}", retry, ex.Message);
            await Task.Delay(5000);
        }
    }
});

await app.RunAsync();