using Wms.Application.DTOS.StockTake;

namespace Wms.Application.Interfaces.Services.StockTake;

public interface IStockTakeService
{
    // Tạo phiếu nháp
    Task<StockTakeDto> CreateAsync(CreateStockTakeDto dto);

    // Bắt đầu kiểm kê: Chốt SystemQty (Snapshot)
    Task<StockTakeDto> StartAsync(Guid id);

    // Cập nhật số lượng đếm được
    Task<StockTakeDto> UpdateCountsAsync(SubmitCountDto dto);

    // Hoàn tất: Tự động điều chỉnh tồn kho dựa trên sai lệch
    Task<StockTakeDto> CompleteAsync(Guid id);

    Task<StockTakeDto> GetByIdAsync(Guid id);
    Task<List<StockTakeDto>> GetListAsync(int page = 1, int pageSize = 20);
}