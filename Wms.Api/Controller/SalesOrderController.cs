using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wms.Application.DTOS.Sales;
using Wms.Application.Interfaces.Service.Sales;
using Wms.Api.Middlewares; // <-- import HasPermission

namespace Wms.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesOrderController : ControllerBase
    {
        private readonly ISalesOrderService _salesOrderService;

        public SalesOrderController(ISalesOrderService salesOrderService)
        {
            _salesOrderService = salesOrderService;
        }

        // CREATE
        [HttpPost]
        [HasPermission("salesorder.create")]
        public async Task<ActionResult<SalesOrderDto>> Create([FromBody] SalesOrderCreateDto dto)
        {
            var result = await _salesOrderService.CreateSOAsync(dto);
            return Ok(result);
        }

        // UPDATE
        [HttpPut("{id}")]
        [HasPermission("salesorder.update")]
        public async Task<ActionResult<SalesOrderDto>> Update(Guid id, [FromBody] SalesOrderUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest("ID mismatch");
            var result = await _salesOrderService.UpdateSOAsync(dto);
            return Ok(result);
        }

        // GET BY ID
        [HttpGet("{id}")]
        [HasPermission("salesorder.view")]
        public async Task<ActionResult<SalesOrderDto>> Get(Guid id)
        {
            var result = await _salesOrderService.GetSOAsync(id);
            return Ok(result);
        }

        // GET LIST / QUERY
        [HttpGet]
        [HasPermission("salesorder.view")]
        public async Task<ActionResult<List<SalesOrderDto>>> Query([FromQuery] SalesOrderQueryDto dto)
        {
            var result = await _salesOrderService.QuerySOsAsync(dto);
            return Ok(result);
        }

        // APPROVE
        [HttpPost("{id}/approve")]
        [HasPermission("salesorder.approve")]
        public async Task<ActionResult<SalesOrderDto>> Approve(Guid id, [FromQuery] Guid managerId)
        {
            var result = await _salesOrderService.ApproveSOAsync(id, managerId);
            return Ok(result);
        }

        // REJECT
        [HttpPost("{id}/reject")]
        [HasPermission("salesorder.reject")]
        public async Task<ActionResult<SalesOrderDto>> Reject(Guid id)
        {
            var result = await _salesOrderService.RejectSOAsync(id);
            return Ok(result);
        }
    }
}
