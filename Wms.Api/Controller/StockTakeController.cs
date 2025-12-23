using Microsoft.AspNetCore.Mvc;
using Wms.Application.DTOS.StockTake;
using Wms.Application.Interfaces.Services.StockTake;

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Mở ra nếu bro đã cài JWT
public class StockTakeController : ControllerBase
{
    private readonly IStockTakeService _stockTakeService;

    public StockTakeController(IStockTakeService stockTakeService)
    {
        _stockTakeService = stockTakeService;
    }

    /// <summary>
    /// Lấy danh sách phiếu kiểm kê (Phân trang)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _stockTakeService.GetListAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết một phiếu kiểm kê kèm danh sách hàng hóa bên trong
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _stockTakeService.GetByIdAsync(id);
        if (result == null) return NotFound("Không tìm thấy phiếu kiểm kê.");
        return Ok(result);
    }

    /// <summary>
    /// Tạo phiếu kiểm kê mới (Trạng thái Draft)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStockTakeDto dto)
    {
        try
        {
            var result = await _stockTakeService.CreateAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Bắt đầu kiểm kê (Chốt số liệu tồn kho hệ thống - Snapshot)
    /// </summary>
    [HttpPost("{id}/start")]
    public async Task<IActionResult> Start(Guid id)
    {
        try
        {
            var result = await _stockTakeService.StartAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lưu số lượng thực tế đếm được cho các mặt hàng
    /// </summary>
    [HttpPost("submit-counts")]
    public async Task<IActionResult> SubmitCounts([FromBody] SubmitCountDto dto)
    {
        try
        {
            var result = await _stockTakeService.UpdateCountsAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Hoàn tất kiểm kê (Tự động điều chỉnh kho dựa trên chênh lệch)
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        try
        {
            var result = await _stockTakeService.CompleteAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}