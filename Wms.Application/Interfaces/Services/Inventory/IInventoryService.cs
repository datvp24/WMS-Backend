using Wms.Application.DTOs.Inventorys;
using Wms.Domain.Enums.Inventory;

namespace Wms.Application.Interfaces.Services.Inventory
{
    public interface IInventoryService
    {
        // =========================
        // GET INVENTORY
        // =========================
        public Task<List<InventoryDto>> GetByProductAsync(int productId)
    => QueryAsync(new InventoryQueryDto { ProductId = productId });

        public Task<List<InventoryDto>> GetByWarehouseAsync(Guid warehouseId)
            => QueryAsync(new InventoryQueryDto { WarehouseId = warehouseId });

        public Task<List<InventoryDto>> GetByLocationAsync(Guid locationId)
            => QueryAsync(new InventoryQueryDto { LocationId = locationId });

        Task<InventoryDto?> GetAsync(Guid id);
        Task<List<InventoryDto>> QueryAsync(InventoryQueryDto dto);


        // =========================
        // INVENTORY HISTORY
        // =========================
        Task<List<InventoryHistoryDto>> GetHistoryAsync(int productId);

        // =========================
        // ADJUST INVENTORY
        // =========================
        Task AdjustAsync(
            Guid warehouseId,
            Guid locationId,
            int productId,
            decimal qtyChange,
            InventoryActionType actionType,
            string? refCode = null,
            string? note = null
        );

        // =========================
        // LOCK / UNLOCK STOCK
        // =========================
        Task LockStockAsync(
            Guid warehouseId,
            Guid locationId,
            int productId,
            decimal qty,
            string? note = null
        );

        Task UnlockStockAsync(
            Guid warehouseId,
            Guid locationId,
            int productId,
            decimal qty,
            string? note = null
        );
    }
}
