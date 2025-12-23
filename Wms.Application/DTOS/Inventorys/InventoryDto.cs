using System;
using Wms.Domain.Enums.Inventory;

namespace Wms.Application.DTOs.Inventorys
{
    public class InventoryDto
    {
        public Guid Id { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid LocationId { get; set; }
        public int ProductId { get; set; }
        public decimal OnHandQuantity { get; set; }
        public decimal LockedQuantity { get; set; }
        public decimal AvailableQuantity => OnHandQuantity - LockedQuantity;
        public decimal InTransitQuantity { get; set; } // optional
    }

    public class InventoryHistoryDto
    {
        public Guid Id { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid LocationId { get; set; }
        public int ProductId { get; set; }
        public decimal QuantityChange { get; set; }
        public InventoryActionType ActionType { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class InventoryAdjustRequest
    {
        public Guid WarehouseId { get; set; }
        public Guid LocationId { get; set; }
        public int ProductId { get; set; }
        public decimal QtyChange { get; set; }
        public InventoryActionType ActionType { get; set; } // enum, không string
        public string? RefCode { get; set; }
        public string? Note { get; set; }
    }

    public class InventoryLockRequest
    {
        public Guid WarehouseId { get; set; }
        public Guid LocationId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public bool Lock { get; set; } = true; // true: lock, false: release
    }

    public class InventoryQueryDto
    {
        public Guid? WarehouseId { get; set; }
        public Guid? LocationId { get; set; }
        public int? ProductId { get; set; }
        public List<int>? ProductIds { get; set; } // mở rộng query nhiều sản phẩm
    }

}
