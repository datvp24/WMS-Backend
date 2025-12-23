using Wms.Application.DTOS.Transfer;

namespace Wms.Application.Interfaces.Services.Transfer;

public interface ITransferService
{
    Task<TransferOrderDto> CreateTransferAsync(TransferOrderDto dto);
    Task<TransferOrderDto> ApproveTransferAsync(Guid transferId);
    Task<TransferOrderDto> CancelTransferAsync(Guid transferId);
    Task<TransferOrderDto> GetTransferByIdAsync(Guid id);
    Task<List<TransferOrderDto>> GetTransfersAsync(int page = 1, int pageSize = 20, string? status = null);
}