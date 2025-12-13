using Microsoft.AspNetCore.Mvc;
using Wms.Api.Middlewares;
using Wms.Application.DTOs.MasterData.Suppliers;
using Wms.Application.Interfaces.Services.MasterData;

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupplierController : ControllerBase
{
    private readonly ISupplierService _service;

    public SupplierController(ISupplierService service)
    {
        _service = service;
    }

    // CREATE
    [HttpPost]
    [HasPermission("supplier.create")]
    public async Task<IActionResult> Create(CreateSupplierDto dto)
        => Ok(await _service.CreateAsync(dto));

    // UPDATE
    [HttpPut("{id}")]
    [HasPermission("supplier.update")]
    public async Task<IActionResult> Update(int id, UpdateSupplierDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return Ok();
    }

    // DELETE
    [HttpDelete("{id}")]
    [HasPermission("supplier.delete")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok();
    }

    // GET BY ID
    [HttpGet("{id}")]
    [HasPermission("supplier.view")]
    public async Task<IActionResult> Get(int id)
        => Ok(await _service.GetAsync(id));

    // GET ALL
    [HttpGet]
    [HasPermission("supplier.view")]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());
}
