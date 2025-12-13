    using Microsoft.AspNetCore.Mvc;
    using Wms.Api.Middlewares;
    using Wms.Application.DTOS.Warehouse;
    using Wms.Application.Interfaces.Services.Warehouse;

    [ApiController]
    [Route("api/warehouses/{warehouseId:guid}/[controller]")]
    public class LocationsController : ControllerBase
    {
        private readonly IWarehouseService _service;

        public LocationsController(IWarehouseService service)
        {
            _service = service;
        }

        // CREATE
        [HttpPost]
        [HasPermission("location.create")]
        public async Task<IActionResult> Create([FromRoute] Guid warehouseId, [FromBody] LocationCreateDto dto)
        {
            if (warehouseId != dto.WarehouseId)
                return BadRequest("WarehouseId mismatch.");

            var res = await _service.CreateLocationAsync(dto);
            return CreatedAtAction(nameof(GetById), new { warehouseId = res.WarehouseId, id = res.Id }, res);
        }

        // LIST BY WAREHOUSE
        [HttpGet]
        [HasPermission("location.view")]
        public async Task<IActionResult> List([FromRoute] Guid warehouseId)
        {
            var res = await _service.GetLocationsByWarehouseAsync(warehouseId);
            return Ok(res);
        }

        // GET BY ID
        [HttpGet("{id}")]
        [HasPermission("location.view")]
        public async Task<IActionResult> GetById([FromRoute] Guid warehouseId, Guid id)
        {
            var res = await _service.GetLocationByIdAsync(id);
            if (res == null || res.WarehouseId != warehouseId)
                return NotFound();

            return Ok(res);
        }

        // UPDATE
        [HttpPut("{id}")]
        [HasPermission("location.update")]
        public async Task<IActionResult> Update([FromRoute] Guid warehouseId, Guid id, [FromBody] LocationUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest();
            var res = await _service.UpdateLocationAsync(dto);
            return Ok(res);
        }

        // DELETE
        [HttpDelete("{id}")]
        [HasPermission("location.delete")]
        public async Task<IActionResult> Delete([FromRoute] Guid warehouseId, Guid id)
        {
            await _service.DeleteLocationAsync(id);
            return NoContent();
        }
    }
