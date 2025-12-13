using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wms.Application.DTOS.Sales;
using Wms.Application.Interfaces.Services.Sales;

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

        [HttpPost]
        public async Task<ActionResult<GoodsIssueDto>> Create([FromBody] GoodsIssueCreateDto dto)
        {
            var result = await _goodsIssueService.CreateGIAsync(dto);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GoodsIssueDto>> Get(Guid id)
        {
            var result = await _goodsIssueService.GetGIAsync(id);
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<List<GoodsIssueDto>>> Query([FromQuery] GoodsIssueQueryDto dto)
        {
            var result = await _goodsIssueService.QueryGIsAsync(dto);
            return Ok(result);
        }

        [HttpPost("{id}/complete")]
        public async Task<ActionResult<GoodsIssueDto>> Complete(Guid id)
        {
            var result = await _goodsIssueService.CompleteGIAsync(id);
            return Ok(result);
        }

        [HttpPost("{id}/cancel")]
        public async Task<ActionResult<GoodsIssueDto>> Cancel(Guid id)
        {
            var result = await _goodsIssueService.CancelGIAsync(id);
            return Ok(result);
        }
    }
}
