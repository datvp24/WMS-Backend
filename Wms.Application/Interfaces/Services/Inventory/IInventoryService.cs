using Wms.Application.DTOs.Inventorys;
using Wms.Domain.Enums.Inventory;

namespace Wms.Application.Interfaces.Services.Inventory
{
    public interface IInventoryService
    {
        Task<InventoryDto?> GetAsync(Guid id);

        Task<List<InventoryDto>> QueryAsync(InventoryQueryDto dto);
        Task<List<InventoryDto>> GetByProductAsync(int productId);
        Task<List<InventoryDto>> GetByWarehouseAsync(Guid warehouseId);
        Task<List<InventoryDto>> GetByLocationAsync(Guid locationId);

        Task<List<InventoryHistoryDto>> GetHistoryAsync(int productId);

        Task AdjustAsync(Guid warehouseId, Guid locationId, int productId, decimal qtyChange, InventoryActionType actionType, string? refCode);

        Task LockStockAsync(Guid warehouseId, Guid locationId, int productId, decimal qty);
        Task UnlockStockAsync(Guid warehouseId, Guid locationId, int productId, decimal qty);
    }
}
