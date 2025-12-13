using Microsoft.AspNetCore.Mvc;
using Wms.Api.Middlewares;
using Wms.Application.DTOs.MasterData.Units;
using Wms.Application.Interfaces.Services.MasterData;

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnitController : ControllerBase
{
    private readonly IUnitService _service;

    public UnitController(IUnitService service)
    {
        _service = service;
    }

    // CREATE
    [HttpPost]
    [HasPermission("unit.create")]
    public async Task<IActionResult> Create(CreateUnitDto dto)
        => Ok(await _service.CreateAsync(dto));

    // UPDATE
    [HttpPut("{id}")]
    [HasPermission("unit.update")]
    public async Task<IActionResult> Update(int id, UpdateUnitDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return Ok();
    }

    // DELETE
    [HttpDelete("{id}")]
    [HasPermission("unit.delete")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok();
    }

    // GET BY ID
    [HttpGet("{id}")]
    [HasPermission("unit.view")]
    public async Task<IActionResult> Get(int id)
        => Ok(await _service.GetAsync(id));

    // GET ALL
    [HttpGet]
    [HasPermission("unit.view")]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());
}
