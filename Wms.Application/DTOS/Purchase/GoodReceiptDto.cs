using Wms.Application.DTOS.Purchase;
using Wms.Domain.Entity.Purchase;
namespace Wms.Application.DTOS.Purchase;
public class GoodsReceiptDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid WarehouseId { get; set; }
    public ReceiptType ReceiptType { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<GoodsReceiptItemDto> Items { get; set; } = new();
    public List<ProductionReceiptItemDto> ProductionReceiptItems { get; set; } = new();

}
public class GRByTypeDto
{
    public ReceiptType ReceiptType { get; set; }
}