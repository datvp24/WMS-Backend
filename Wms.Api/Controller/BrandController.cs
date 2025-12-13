using Microsoft.AspNetCore.Mvc;
using Wms.Api.Middlewares;
using Wms.Application.DTOs.MasterData.Brands;
using Wms.Application.Interfaces.Services.MasterData;

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandController : ControllerBase
{
    private readonly IBrandService _service;

    public BrandController(IBrandService service)
    {
        _service = service;
    }

    // CREATE
    [HttpPost]
    [HasPermission("brand.create")]
    public async Task<IActionResult> Create(CreateBrandDto dto)
        => Ok(await _service.CreateAsync(dto));

    // UPDATE
    [HttpPut("{id}")]
    [HasPermission("brand.update")]
    public async Task<IActionResult> Update(int id, UpdateBrandDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return Ok();
    }

    // DELETE
    [HttpDelete("{id}")]
    [HasPermission("brand.delete")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok();
    }

    // GET BY ID
    [HttpGet("{id}")]
    [HasPermission("brand.view")]
    public async Task<IActionResult> Get(int id)
        => Ok(await _service.GetAsync(id));

    // GET ALL
    [HttpGet]
    [HasPermission("brand.view")]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());
}
