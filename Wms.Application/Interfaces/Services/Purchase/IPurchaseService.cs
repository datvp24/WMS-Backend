using Wms.Application.DTOS.Purchase;
using Wms.Domain.Entity.Purchase;
namespace Wms.Application.Interfaces.Services.Purchase;
public interface IPurchaseService
{
    // PURCHASE ORDER
    Task<PurchaseOrderDto> CreatePOAsync(PurchaseOrderDto dto);
    Task<PurchaseOrderDto> ApprovePOAsync(Guid poId);
    Task<PurchaseOrderDto> RejectPOAsync(Guid poId);
    Task<List<PurchaseOrderDto>> GetPOsAsync(int page = 1, int pageSize = 20, string? status = null);
    Task<PurchaseOrderDto> GetPOAsync(Guid poId);
    Task<PurchaseOrderDto> GetPOM0Async(Guid poId);


    // GOODS RECEIPT
    Task<GoodsReceiptDto> CreateGRAsync(GoodsReceiptDto dto);
    Task<GoodsReceiptDto> ApproveProductionReceipt(GoodsReceiptDto dto);
    Task<GoodsReceiptDto> CountingReceiptProduction(GoodsReceiptDto dto);
    Task IncomingStockCount(GoodsReceiptItem1Dto dto);
    Task<List<GoodsReceiptDto>> GetGRsAsync(Guid? poId = null, int page = 1, int pageSize = 20);
    Task CancelGRAsync(Guid grId);
    Task<List<GoodsReceipt>> getGRbytype(GRByTypeDto dto);
}
