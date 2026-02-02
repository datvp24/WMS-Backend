using Microsoft.AspNetCore.Mvc;
using Wms.Application.DTOS.Purchase;
using Wms.Application.Interfaces.Services.Purchase;
using Wms.Api.Middlewares;
using Wms.Domain.Entity.Purchase;

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
    [HttpGet("pom0/{poId}")]
    [HasPermission("purchase.po.view")]
    public async Task<ActionResult<PurchaseOrderDto>> GetPOM0ById(Guid poId)
    {
        // Gọi trực tiếp hàm lấy chi tiết để Service tính toán ReceivedQuantity
        var po = await _purchaseService.GetPOM0Async(poId);

        if (po == null) return NotFound();
        return Ok(po);
    }
    [HttpGet("grbytype")]
    [HasPermission("purchase.po.view")]
    public async Task<ActionResult<List<GoodsReceiptDto>>> GetGRsByType(
    [FromQuery] GRByTypeDto dto)
    {
        var entities = await _purchaseService.getGRbytype(dto);

        // ✅ Map sang DTO để tránh circular reference
        var result = entities.Select(gr => new GoodsReceiptDto
        {
            Id = gr.Id,
            Code = gr.Code,
            PurchaseOrderId = gr.PurchaseOrderId,
            WarehouseId = gr.WarehouseId,
            ReceiptType = gr.ReceiptType,
            Status = gr.Status,
            CreatedAt = gr.CreatedAt,
            UpdatedAt = gr.UpdatedAt,

            // ✅ Map Items - CHỈ LẤY DATA CẦN THIẾT
            Items = gr.Items.Select(i => new GoodsReceiptItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Received_Qty = i.Received_Qty,
                Status = i.Status,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
                // ❌ KHÔNG map GoodsReceipt để tránh circular
            }).ToList(),

            // ✅ Map ProductionReceiptItems
            ProductionReceiptItems = gr.Productions.Select(p => new ProductionReceiptItemDto
            {
                Id = p.Id,
                ProductId = p.ProductId,
                Quantity = p.Quantity,
                Receipt_Qty = p.Receipt_Qty,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList()

        }).ToList();

        return Ok(result);
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
    [HttpPost("receive-item")]
    [HasPermission("purchase.gr.receive")]
    public async Task<IActionResult> ReceiveItem([FromBody] GoodsReceiptItem1Dto dto)
    {
        // 1. Kiểm tra đầu vào cơ bản
        if (dto == null || dto.Received_Qty <= 0)
        {
            return BadRequest("Số lượng nhập kho phải lớn hơn 0.");
        }

        try
        {
            // 2. Gọi hàm xử lý logic mà chúng ta đã viết
            await _purchaseService.IncomingStockCount(dto);

            // 3. Trả về kết quả thành công
            return Ok(new { message = "Cập nhật số lượng nhập kho thành công." });
        }
        catch (Exception ex)
        {
            // 4. Xử lý lỗi nếu không tìm thấy hàng hoặc lỗi hệ thống
            // Bạn có thể log lỗi ở đây (logger.LogError...)
            return BadRequest(new { message = ex.Message });
        }
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
    [HttpPost("gr-production-approve")]
    [HasPermission("purchase.gr.approve")]
    public async Task<ActionResult<GoodsReceiptDto>> ApproveGRProduction([FromBody] GoodsReceiptDto dto)
    {
        var gr = await _purchaseService.ApproveProductionReceipt(dto);
        return gr;
    }
    [HttpPost("gr-production-counting")]
    [HasPermission("purchase.gr.Counting")]
    public async Task<ActionResult<GoodsReceiptDto>> CountingGRProduction([FromBody] GoodsReceiptDto dto)
    {
        var gr = await _purchaseService.CountingReceiptProduction(dto);
        return gr;
    }

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
