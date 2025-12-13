using Wms.Domain.Entity.Auth;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Purchase;
namespace Wms.Domain.Entity.Purchase;
public class PurchaseOrder 
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreateBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public List<GoodsReceipt> GoodsReceipts { get; set; } = new(); // ✅ thêm


    public List<PurchaseOrderItem> Items { get; set; } = new();
}