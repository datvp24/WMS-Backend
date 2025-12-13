using Microsoft.AspNetCore.Mvc;
using Wms.Api.Middlewares;
using Wms.Application.DTOs.MasterData.Customers;
using Wms.Application.Interfaces.Services.MasterData;

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _service;

    public CustomerController(ICustomerService service)
    {
        _service = service;
    }

    // CREATE
    [HttpPost]
    [HasPermission("customer.create")]
    public async Task<IActionResult> Create(CreateCustomerDto dto)
        => Ok(await _service.CreateAsync(dto));

    // UPDATE
    [HttpPut("{id}")]
    [HasPermission("customer.update")]
    public async Task<IActionResult> Update(int id, UpdateCustomerDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return Ok();
    }

    // DELETE
    [HttpDelete("{id}")]
    [HasPermission("customer.delete")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok();
    }

    // GET BY ID
    [HttpGet("{id}")]
    [HasPermission("customer.view")]
    public async Task<IActionResult> Get(int id)
        => Ok(await _service.GetAsync(id));

    // GET ALL
    [HttpGet]
    [HasPermission("customer.view")]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());
}
