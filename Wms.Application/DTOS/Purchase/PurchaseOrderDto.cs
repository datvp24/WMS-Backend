using Wms.Application.DTOS.Purchase;

namespace Wms.Application.DTOS.Purchase;

public class PurchaseOrderDto
{
    public Guid? Id { get; set; }
    public string Code { get; set; }          // Mã PO
    public int SupplierId { get; set; }
    public string? Status { get; set; }        // Pending, Approved, Rejected
    public DateTime? CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; } // Nullable vì chưa approve
    public List<PurchaseOrderItemDto> Items { get; set; } = new();
}