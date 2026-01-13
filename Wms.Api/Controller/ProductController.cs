using Microsoft.AspNetCore.Mvc;
using Wms.Api.Middlewares;
using Wms.Application.DTOs.MasterData.Products;
using Wms.Application.Interfaces.Services.MasterData;
using Wms.Application.Services.MasterData;

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductService _service;

    public ProductController(IProductService service)
    {
        _service = service;
    }

    // CREATE
    [HttpPost]
    [HasPermission("product.create")]
    public async Task<IActionResult> Create(CreateProductDto dto)
        => Ok(await _service.CreateAsync(dto));

    // UPDATE
    [HttpPut("{id}")]
    [HasPermission("product.update")]
    public async Task<IActionResult> Update(int id, UpdateProductDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return Ok();
    }

    // DELETE
    [HttpDelete("{id}")]
    [HasPermission("product.delete")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok();
    }

    // GET BY ID
    [HttpGet("{id}")]
    [HasPermission("product.view")]
    public async Task<IActionResult> Get(int id)
        => Ok(await _service.GetAsync(id));

    // GET ALL
    [HttpGet]
    [HasPermission("product.view")]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    [HasPermission("product.view")]
    [HttpPost("by-type")]
    public async Task<IActionResult> GetByType([FromBody] ProductTypeDto dto)
    {
        if (dto == null)
            return BadRequest("DTO không được null");

        try
        {
            var products = await _service.GetAllByType(dto);
            return Ok(products); 
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }
    [HttpGet("By-Supplier/{supplierId}")]
    [HasPermission("product.view")]
    public async Task<IActionResult> GetAllBySup(int supplierId)
        => Ok(await _service.GetAllBySupplierAsync(supplierId));


    // FILTER
    [HttpPost("filter")]
    [HasPermission("product.view")]
    public async Task<IActionResult> Filter(ProductFilterDto dto)
        => Ok(await _service.FilterAsync(dto));
}
