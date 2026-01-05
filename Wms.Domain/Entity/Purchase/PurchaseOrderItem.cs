using Wms.Domain.Entity.Warehouses;

namespace Wms.Domain.Entity.Purchase;
public class PurchaseOrderItem
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int Received_qty { get; set; }
    public Status Status { get; set; } = Status.Pending;
    public decimal Price { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; }
}
public enum Status
{
    Pending = 0,
    Approve = 1,
    Partically_Received = 2,
    Complete = 3,
    Rejected = 4,
}