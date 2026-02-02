using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wms.Api.Middlewares; // <-- import HasPermission
using Wms.Application.DTOS.Sales;
using Wms.Application.Interfaces.Service.Sales;
using Wms.Application.Interfaces.Services.Sales;
using Wms.Domain.Entity.Sales;

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
        public async Task<ActionResult<SalesOrderDto>> Create([FromBody] SalesOrderDto dto)
        {
            var result = await _salesOrderService.CreateSOAsync(dto);
            return Ok(result);
        }
        [HasPermission("salesorder.view")]
        [HttpGet("goods-issue/{id}")]
        public async Task<ActionResult<GoodsIssueDetailDto>> GetGoodsIssue(Guid id)
        {
            var result = await _salesOrderService.GetGoodsIssueDetailAsync(id);
            if (result == null)
                return NotFound("Goods Issue not found");

            return Ok(result);
        }

        [HasPermission("salesorder.view")]
        [HttpGet("goodsissues")]
        public async Task<ActionResult<List<GoodsIssueDto>>> QueryGoodsIssues([FromQuery] GoodsIssueQuery1Dto dto)
        {
            var result = await _salesOrderService.QueryGoodsIssuesAsync(dto);
            return Ok(result);
        }

        // UPDATE

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
        public async Task<ActionResult<SalesOrderDto>> Approve(Guid id)
        {
            var result = await _salesOrderService.ApproveSOAsync(id);
            return Ok(result);
        }
        [HttpPost("issue")]
        [HasPermission("salesorder.Issue")]
        public async Task<IActionResult> Issue([FromBody] IssueGoodsDto dto)
        {
                await _salesOrderService.OutgoingStockCount(dto);
                return Ok(new { Message = "Issued successfully" });
        }
        [HttpPost("production")]
        public async Task<IActionResult> CreateProductionGI(
        [FromBody] ProductionGoodsIssueCreateDto dto)
        {
            var gi = await _salesOrderService.CreateProductionGIAsync(dto);
            return Ok(gi);
        }


        /// <summary>
        /// Duyệt phiếu xuất kho (Sale / Production)
        /// </summary>
        [HttpPost("GI/{giId}/approve")]
        public async Task<ActionResult<GoodsIssueDto>> ApproveGI(Guid giId)
        {
            var result = await _salesOrderService.ApproveGIAsync(giId);
            return Ok(result);
        }
        [HttpPost("picking")]
        [HasPermission("salesorder.picking")]
        public async Task<IActionResult> Picking([FromBody] GoodsIssueItemDto dto)
        {
            await _salesOrderService.Picking(dto);
            return Ok();
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
