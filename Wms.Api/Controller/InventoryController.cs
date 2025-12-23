using Microsoft.AspNetCore.Mvc;
using Wms.Application.DTOs.Inventorys;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Domain.Enums.Inventory;
using Wms.Api.Middlewares;

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;
    public InventoryController(IInventoryService service) => _service = service;

    // =========================
    // GET INVENTORY BY ID
    // =========================
    [HttpGet("{id:guid}")]
    [HasPermission("inventory.view")]
    public async Task<IActionResult> Get(Guid id)
    {
        var inv = await _service.GetAsync(id);
        if (inv == null) return NotFound();
        return Ok(inv);
    }

    // =========================
    // QUERY INVENTORY
    // =========================
    [HttpGet]
    [HasPermission("inventory.view")]
    public async Task<IActionResult> Query(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? locationId,
        [FromQuery] int? productId,
        [FromQuery] List<int>? productIds)
    {
        var dto = new InventoryQueryDto
        {
            WarehouseId = warehouseId,
            LocationId = locationId,
            ProductId = productId,
            ProductIds = productIds
        };
        var list = await _service.QueryAsync(dto);
        return Ok(list);
    }

    // =========================
    // INVENTORY HISTORY
    // =========================
    [HttpGet("product/{productId}/history")]
    [HasPermission("inventory.history")]
    public async Task<IActionResult> History(int productId)
    {
        var list = await _service.GetHistoryAsync(productId);
        return Ok(list);
    }

    // =========================
    // ADJUST INVENTORY (Receive / Issue / Transfer / Adjust)
    // =========================
    [HttpPost("adjust")]
    [HasPermission("inventory.adjust")]
    public async Task<IActionResult> Adjust([FromBody] InventoryAdjustRequest req)
    {
        await _service.AdjustAsync(
            req.WarehouseId,
            req.LocationId,
            req.ProductId,
            req.QtyChange,
            req.ActionType,
            req.RefCode,
            req.Note
        );
        return Ok();
    }

    // =========================
    // LOCK OR UNLOCK STOCK
    // =========================
    [HttpPost("lock-toggle")]
    [HasPermission("inventory.lock")]
    public async Task<IActionResult> LockToggle([FromBody] InventoryLockRequest req)
    {
        if (req.Lock)
            await _service.LockStockAsync(req.WarehouseId, req.LocationId, req.ProductId, req.Quantity, note: null);
        else
            await _service.UnlockStockAsync(req.WarehouseId, req.LocationId, req.ProductId, req.Quantity, note: null);

        return Ok();
    }

    // =========================
    // HELPER ENDPOINTS (OPTIONAL)
    // =========================
    [HttpGet("warehouse/{warehouseId}")]
    [HasPermission("inventory.view")]
    public Task<IActionResult> GetByWarehouse(Guid warehouseId)
        => GetQueryResult(_service.GetByWarehouseAsync(warehouseId));

    [HttpGet("location/{locationId}")]
    [HasPermission("inventory.view")]
    public Task<IActionResult> GetByLocation(Guid locationId)
        => GetQueryResult(_service.GetByLocationAsync(locationId));

    [HttpGet("product/{productId}")]
    [HasPermission("inventory.view")]
    public Task<IActionResult> GetByProduct(int productId)
        => GetQueryResult(_service.GetByProductAsync(productId));

    // Helper private method
    private async Task<IActionResult> GetQueryResult(Task<List<InventoryDto>> task)
    {
        var list = await task;
        return Ok(list);
    }
}
