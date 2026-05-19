// ============================================================
// File: Wms.Application/DTOs/WebSocket/InventoryChangeEvent.cs
// ============================================================
namespace Wms.Application.DTOs.WebSocket;

public class InventoryChangeEvent
{
    public string Type { get; init; } = "inventory_change";

    public int ProductId { get; init; }
    public string ProductName { get; init; } = "";
    public string ProductCode { get; init; } = "";

    public string LocationCode { get; init; } = "";
    public string WarehouseName { get; init; } = "";
    public string LotCode { get; init; } = "N/A";

    /// <summary>"onHandQuantity" | "lockedQuantity" | "availableQuantity"</summary>
    public string Field { get; init; } = "";

    public decimal OldValue { get; init; }
    public decimal NewValue { get; init; }
    public decimal Delta { get; init; }

    public string ChangedAt { get; init; } = DateTime.UtcNow.ToString("o");
}