using Microsoft.AspNetCore.Mvc;
using Wms.Application.DTOS.Purchase;
using Wms.Application.Interfaces.Services.Purchase;
using Wms.Api.Middlewares;

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;

    public PurchaseController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    // ========================
    // PURCHASE ORDER
    // ========================

    [HttpPost("po")]
    [HasPermission("purchase.po.create")]
    public async Task<ActionResult<PurchaseOrderDto>> CreatePO([FromBody] PurchaseOrderDto dto)
    {
        var po = await _purchaseService.CreatePOAsync(dto);
        return CreatedAtAction(nameof(GetPOById), new { poId = po.Id }, po);
    }

    [HttpGet("po/{poId}")]
    [HasPermission("purchase.po.view")]
    public async Task<ActionResult<PurchaseOrderDto>> GetPOById(Guid poId)
    {
        // Gọi trực tiếp hàm lấy chi tiết để Service tính toán ReceivedQuantity
        var po = await _purchaseService.GetPOAsync(poId);

        if (po == null) return NotFound();
        return Ok(po);
    }

    [HttpGet("po")]
    [HasPermission("purchase.po.view")]
    public async Task<ActionResult<List<PurchaseOrderDto>>> GetPOs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var pos = await _purchaseService.GetPOsAsync(page, pageSize, status);
        return Ok(pos);
    }

    [HttpPost("po/{poId}/approve")]
    [HasPermission("purchase.po.approve")]
    public async Task<ActionResult<PurchaseOrderDto>> ApprovePO(Guid poId)
    {
        var po = await _purchaseService.ApprovePOAsync(poId);
        return Ok(po);
    }

    [HttpPost("po/{poId}/reject")]
    [HasPermission("purchase.po.reject")]
    public async Task<ActionResult<PurchaseOrderDto>> RejectPO(Guid poId)
    {
        var po = await _purchaseService.RejectPOAsync(poId);
        return Ok(po);
    }

    // ========================
    // GOODS RECEIPT
    // ========================

    [HttpPost("gr")]
    [HasPermission("purchase.gr.create")]
    public async Task<ActionResult<GoodsReceiptDto>> CreateGR([FromBody] GoodsReceiptDto dto)
    {
        var gr = await _purchaseService.CreateGRAsync(dto);
        return CreatedAtAction(nameof(GetGRById), new { grId = gr.Id }, gr);
    }

    [HttpGet("gr/{grId}")]
    [HasPermission("purchase.gr.view")]
    public async Task<ActionResult<GoodsReceiptDto>> GetGRById(Guid grId)
    {
        var grs = await _purchaseService.GetGRsAsync();
        var gr = grs.FirstOrDefault(x => x.Id == grId);
        if (gr == null) return NotFound();
        return Ok(gr);
    }
    

    [HttpGet("gr")]
    [HasPermission("purchase.gr.view")]
    public async Task<ActionResult<List<GoodsReceiptDto>>> GetGRs(
        [FromQuery] Guid? poId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var grs = await _purchaseService.GetGRsAsync(poId, page, pageSize);
        return Ok(grs);
    }

    [HttpDelete("gr/{grId}")]
    [HasPermission("purchase.gr.cancel")]
    public async Task<IActionResult> CancelGR(Guid grId)
    {
        await _purchaseService.CancelGRAsync(grId);
        return NoContent();
    }
}
