namespace Wms.Application.DTOS.StockTake;

public class CreateStockTakeDto
{
    public Guid WarehouseId { get; set; }
    public string? Description { get; set; }

    // Nếu muốn kiểm kê theo vùng (Zone) hoặc một số Location nhất định
    // public List<Guid>? SpecificLocationIds { get; set; } 
}