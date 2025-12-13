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

    [HttpGet("{id:guid}")]
    [HasPermission("inventory.view")]
    public async Task<IActionResult> Get(Guid id) => Ok(await _service.GetAsync(id));

    [HttpGet]
    [HasPermission("inventory.view")]
    public async Task<IActionResult> Query([FromQuery] Guid? warehouseId, [FromQuery] Guid? locationId, [FromQuery] int? productId)
        => Ok(await _service.QueryAsync(new InventoryQueryDto { WarehouseId = warehouseId, LocationId = locationId, ProductId = productId }));

    [HttpGet("product/{productId}/history")]
    [HasPermission("inventory.history")]
    public async Task<IActionResult> History(int productId) => Ok(await _service.GetHistoryAsync(productId));

    [HttpPost("adjust")]
    [HasPermission("inventory.adjust")]
    public async Task<IActionResult> Adjust([FromBody] InventoryAdjustRequest req)
    {
        if (!Enum.TryParse<InventoryActionType>(req.Action, true, out var actionType))
            return BadRequest("Invalid action type");

        await _service.AdjustAsync(req.WarehouseId, req.LocationId, req.ProductId, req.QtyChange, actionType, req.RefCode);
        return Ok();
    }

    [HttpPost("lock")]
    [HasPermission("inventory.lock")]
    public async Task<IActionResult> Lock([FromBody] InventoryLockRequest req)
    {
        await _service.LockStockAsync(req.WarehouseId, req.LocationId, req.ProductId, req.Quantity);
        return Ok();
    }

    [HttpPost("unlock")]
    [HasPermission("inventory.unlock")]
    public async Task<IActionResult> Unlock([FromBody] InventoryLockRequest req)
    {
        await _service.UnlockStockAsync(req.WarehouseId, req.LocationId, req.ProductId, req.Quantity);
        return Ok();
    }
}
