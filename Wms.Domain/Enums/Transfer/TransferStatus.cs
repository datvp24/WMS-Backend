namespace Wms.Domain.Enums.Transfer
{
    public enum TransferStatus
    {
        Draft = 1,        // Mới tạo
        Submitted = 2,    // Gửi duyệt
        Approved = 3,     // Đã duyệt
        Rejected = 4,     // Bị từ chối
        Completed = 5,    // Đã chuyển xong
        Cancelled = 6
    }
}
