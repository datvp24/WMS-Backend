namespace Wms.Domain.Entity.Purchase;
public class GoodsReceiptItem
{
    public Guid Id { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public Guid LocationId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public GoodsReceipt GoodsReceipt { get; set; }
}