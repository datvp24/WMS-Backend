using Wms.Application.DTOS.Purchase;
namespace Wms.Application.DTOS.Purchase;
public class GoodsReceiptDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<GoodsReceiptItemDto> Items { get; set; } = new();
}