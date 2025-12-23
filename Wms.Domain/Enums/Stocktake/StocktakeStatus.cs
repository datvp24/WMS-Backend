namespace Wms.Domain.Enums.StockTakes;

public enum StockTakeStatus
{
    Draft = 0,      // Mới tạo, đang chọn sản phẩm/vị trí kiểm kê
    InProgress = 1, // Đang trong quá trình đếm hàng (Lock kho/location)
    Completed = 2,  // Đã đối soát và cập nhật tồn kho (Adjustment xong)
    Cancelled = 3   // Hủy đợt kiểm kê
}