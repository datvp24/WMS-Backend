// ============================================================
// File: Wms.Infrastructure/WebSockets/InventoryWebSocketHub.cs
// ============================================================
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Wms.Infrastructure.WebSockets;

/// <summary>
/// Singleton hub quản lý tất cả WS connections đang mở.
/// TransferService / InventoryService gọi BroadcastAsync() sau mỗi thay đổi.
/// </summary>
public class InventoryWebSocketHub
{
    // ── Danh sách socket đang kết nối ──────────────────────────────
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

    // ── Channel để decouple: producer (service) → consumer (broadcast loop) ──
    private readonly Channel<object> _channel =
        Channel.CreateBounded<object>(new BoundedChannelOptions(512)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

    public ChannelWriter<object> Writer => _channel.Writer;

    // ─────────────────────────────────────────────────────────────
    // Đăng ký / huỷ socket
    // ─────────────────────────────────────────────────────────────
    public string Register(WebSocket ws)
    {
        var id = Guid.NewGuid().ToString("N");
        _sockets[id] = ws;
        return id;
    }

    public void Unregister(string id) => _sockets.TryRemove(id, out _);

    // ─────────────────────────────────────────────────────────────
    // Broadcast 1 payload JSON đến tất cả client
    // ─────────────────────────────────────────────────────────────
    public async Task BroadcastAsync(object payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        var dead = new List<string>();

        foreach (var (id, ws) in _sockets)
        {
            try
            {
                if (ws.State == WebSocketState.Open)
                    await ws.SendAsync(segment, WebSocketMessageType.Text, true, ct);
                else
                    dead.Add(id);
            }
            catch
            {
                dead.Add(id);
            }
        }

        foreach (var id in dead) _sockets.TryRemove(id, out _);
    }

    // ─────────────────────────────────────────────────────────────
    // Background loop: đọc từ Channel → broadcast
    // ─────────────────────────────────────────────────────────────
    public async Task RunBroadcastLoopAsync(CancellationToken ct)
    {
        await foreach (var payload in _channel.Reader.ReadAllAsync(ct))
        {
            await BroadcastAsync(payload, ct);
        }
    }
}