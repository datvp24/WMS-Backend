// ============================================================
// File: Wms.Infrastructure/WebSockets/InventoryBroadcastWorker.cs
// ============================================================
using Microsoft.Extensions.Hosting;
namespace Wms.Infrastructure.WebSockets;

public class InventoryBroadcastWorker : BackgroundService
{
    private readonly InventoryWebSocketHub _hub;

    public InventoryBroadcastWorker(InventoryWebSocketHub hub)
    {
        _hub = hub;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => _hub.RunBroadcastLoopAsync(stoppingToken);
}