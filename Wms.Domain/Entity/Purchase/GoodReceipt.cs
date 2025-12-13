using Wms.Domain.Entity.Purchase;
namespace Wms.Domain.Entity.Purchase;
public class GoodsReceipt
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid WarehouseId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<GoodsReceiptItem> Items { get; set; } = new();
    public PurchaseOrder PurchaseOrder { get; set; }
}