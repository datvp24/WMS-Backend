using Wms.Application.DTOS.Purchase;
namespace Wms.Application.Interfaces.Services.Purchase;
public interface IPurchaseService
{
    // PURCHASE ORDER
    Task<PurchaseOrderDto> CreatePOAsync(PurchaseOrderDto dto);
    Task<PurchaseOrderDto> ApprovePOAsync(Guid poId);
    Task<PurchaseOrderDto> RejectPOAsync(Guid poId);
    Task<List<PurchaseOrderDto>> GetPOsAsync(int page = 1, int pageSize = 20, string? status = null);
    Task<PurchaseOrderDto> GetPOAsync(Guid poId);

    // GOODS RECEIPT
    Task<GoodsReceiptDto> CreateGRAsync(GoodsReceiptDto dto);
    Task<List<GoodsReceiptDto>> GetGRsAsync(Guid? poId = null, int page = 1, int pageSize = 20);
    Task CancelGRAsync(Guid grId);
}
