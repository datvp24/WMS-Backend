using Wms.Domain.Entity.Purchase;

namespace Wms.Application.DTOS.Purchase;

public class PurchaseOrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public Guid WarehouseId {  get; set; }
    public decimal Price { get; set; }
    public Status Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}