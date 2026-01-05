using Wms.Domain.Enums.Purchase;

namespace Wms.Application.DTOS.Purchase;

public class GoodsReceiptItemDto
{
    public Guid Id {  get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int Received_Qty { get; set; }
    public GRIStatus Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
public class GoodsReceiptItem1Dto
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int Received_Qty { get; set; }
    public GRIStatus Status { get; set; }

}