using System;
using Wms.Domain.Enums.Inventory;
using Wms.Domain.Enums.location;

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
        public LocationType? LocationType { get; set; }

    }
    public class GetAvailableLocationsRequest
    {
        public int ProductId { get; set; }
        public Guid WarehouseId { get; set; }
    }
    public class PutawayDto
    {
        public int ProductId { get; set; }      // Sản phẩm cần putaway
        public Guid FromLocationId { get; set; } // Thường là Receiving location
        public Guid ToLocationId { get; set; }   // Storage location
        public Guid WarehouseId { get; set; }
        public decimal Qty { get; set; }         // Số lượng putaway
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
