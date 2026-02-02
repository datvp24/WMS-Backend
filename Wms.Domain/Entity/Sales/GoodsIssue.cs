using System;
using System.Collections.Generic;
using Wms.Domain.Entity.Warehouses;

namespace Wms.Domain.Entity.Sales
{
    public class GoodsIssue
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? SalesOrderId { get; set; }
        public SalesOrder? SalesOrder { get; set; } = null!;
        public GIType Type { get;set; }
        public string Code { get; set; } = null!;
        public Guid WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!; 
        public GIStatus Status { get; set; }
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow; // EF sẽ tự điền giá trị
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }

        public ICollection<GoodsIssueItem> Items { get; set; } = new List<GoodsIssueItem>();
    }
    public enum GIStatus
    {
        Pending = 0,
        Approve = 1,
        Partically_Issued = 2,
        Complete = 3,
        Rejected = 4,
        Picking = 5,
    }
    public enum GIType
    {
        Sale,
        Production
    }
}
