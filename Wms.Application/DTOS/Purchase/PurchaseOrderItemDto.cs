namespace Wms.Application.DTOS.Purchase;

public class PurchaseOrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}