using Wms.Domain.Entity.Purchase;
namespace Wms.Domain.Entity.Purchase;
public class GoodsReceipt
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid WarehouseId { get; set; }
    public ReceiptType ReceiptType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime ReceivedAt { get; set; }
    public Status Status { get; set; } = Status.Pending;
    public List<GoodsReceiptItem> Items { get; set; } = new();
    public List<ProductionReceiptItem> Productions { get; set; } = new();
    public PurchaseOrder? PurchaseOrder { get; set; }
}
public enum ReceiptType
{
    Purchase,
    Production
}