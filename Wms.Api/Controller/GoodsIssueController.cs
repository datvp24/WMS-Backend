using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wms.Application.DTOS.Sales;
using Wms.Application.Interfaces.Services.Sales;
using Wms.Api.Middlewares; // <-- import HasPermission attribute

namespace Wms.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GoodsIssueController : ControllerBase
    {
        private readonly IGoodsIssueService _goodsIssueService;

        public GoodsIssueController(IGoodsIssueService goodsIssueService)
        {
            _goodsIssueService = goodsIssueService;
        }

        // CREATE
        [HttpPost]
        [HasPermission("goodsissue.create")]
        public async Task<ActionResult<GoodsIssueDto>> Create([FromBody] GoodsIssueCreateDto dto)
        {
            var result = await _goodsIssueService.CreateGIAsync(dto);
            return Ok(result);
        }

        // GET BY ID
        [HttpGet("{id}")]
        [HasPermission("goodsissue.view")]
        public async Task<ActionResult<GoodsIssueDto>> Get(Guid id)
        {
            var result = await _goodsIssueService.GetGIAsync(id);
            return Ok(result);
        }

        // QUERY LIST
        [HttpGet]
        [HasPermission("goodsissue.view")]
        public async Task<ActionResult<List<GoodsIssueDto>>> Query([FromQuery] GoodsIssueQueryDto dto)
        {
            var result = await _goodsIssueService.QueryGIsAsync(dto);
            return Ok(result);
        }

        // COMPLETE GI
        [HttpPost("{id}/complete")]
        [HasPermission("goodsissue.complete")]
        public async Task<ActionResult<GoodsIssueDto>> Complete(Guid id)
        {
            var result = await _goodsIssueService.CompleteGIAsync(id);
            return Ok(result);
        }

        // CANCEL GI
        [HttpPost("{id}/cancel")]
        [HasPermission("goodsissue.cancel")]
        public async Task<ActionResult<GoodsIssueDto>> Cancel(Guid id)
        {
            var result = await _goodsIssueService.CancelGIAsync(id);
            return Ok(result);
        }
    }
}
