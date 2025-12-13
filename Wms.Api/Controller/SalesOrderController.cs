using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wms.Application.DTOS.Sales;
using Wms.Application.Interfaces.Service.Sales;

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

        [HttpPost]
        public async Task<ActionResult<SalesOrderDto>> Create([FromBody] SalesOrderCreateDto dto)
        {
            var result = await _salesOrderService.CreateSOAsync(dto);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<SalesOrderDto>> Update(Guid id, [FromBody] SalesOrderUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest("ID mismatch");
            var result = await _salesOrderService.UpdateSOAsync(dto);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SalesOrderDto>> Get(Guid id)
        {
            var result = await _salesOrderService.GetSOAsync(id);
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<List<SalesOrderDto>>> Query([FromQuery] SalesOrderQueryDto dto)
        {
            var result = await _salesOrderService.QuerySOsAsync(dto);
            return Ok(result);
        }

        [HttpPost("{id}/approve")]
        public async Task<ActionResult<SalesOrderDto>> Approve(Guid id, [FromQuery] Guid managerId)
        {
            var result = await _salesOrderService.ApproveSOAsync(id, managerId);
            return Ok(result);
        }

        [HttpPost("{id}/reject")]
        public async Task<ActionResult<SalesOrderDto>> Reject(Guid id)
        {
            var result = await _salesOrderService.RejectSOAsync(id);
            return Ok(result);
        }
    }
}
