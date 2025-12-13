using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wms.Domain.Entity.Inventorys;

[Table("Inventories")]
public class Inventory
{
    [Key]
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid LocationId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }          // tổng tồn
    public decimal LockedQuantity { get; set; }    // đã khóa (SO chưa xuất)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}