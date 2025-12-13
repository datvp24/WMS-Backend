using Microsoft.AspNetCore.Mvc;
using Wms.Api.Middlewares;
using Wms.Application.DTOS.Warehouse;
using Wms.Application.Interfaces.Services.Warehouse;

[ApiController]
[Route("api/[controller]")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _service;

    public WarehousesController(IWarehouseService service)
    {
        _service = service;
    }

    // CREATE
    [HttpPost]
    [HasPermission("warehouse.create")]
    public async Task<IActionResult> Create([FromBody] WarehouseCreateDto dto)
    {
        var res = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
    }

    // UPDATE
    [HttpPut("{id:guid}")]
    [HasPermission("warehouse.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] WarehouseUpdateDto dto)
    {
        dto.Id = id;
        var res = await _service.UpdateAsync(dto);
        return Ok(res);
    }


    // GET BY ID
    [HttpGet("{id:guid}")]
    [HasPermission("warehouse.view")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var res = await _service.GetByIdAsync(id);
        if (res == null) return NotFound();
        return Ok(res);
    }

    // QUERY + SEARCH + PAGINATION
    [HttpGet]
    [HasPermission("warehouse.view")]
    public async Task<IActionResult> Query(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string q = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] bool asc = false)
    {
        var (items, total) = await _service.QueryAsync(page, pageSize, q, sortBy, asc);
        return Ok(new { items, total });
    }

    // DELETE
    [HttpDelete("{id:guid}")]
    [HasPermission("warehouse.delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    // LOCK
    [HttpPost("{id:guid}/lock")]
    [HasPermission("warehouse.lock")]
    public async Task<IActionResult> Lock(Guid id)
    {
        await _service.LockAsync(id);
        return NoContent();
    }

    // UNLOCK
    [HttpPost("{id:guid}/unlock")]
    [HasPermission("warehouse.unlock")]
    public async Task<IActionResult> Unlock(Guid id)
    {
        await _service.UnlockAsync(id);
        return NoContent();
    }
}
