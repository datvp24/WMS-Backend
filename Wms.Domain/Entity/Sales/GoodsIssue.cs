using System;
using System.Collections.Generic;
using Wms.Domain.Entity.Warehouses;

namespace Wms.Domain.Entity.Sales
{
    public class GoodsIssue
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SalesOrderId { get; set; }
        public SalesOrder SalesOrder { get; set; } = null!;

        public string Code { get; set; } = null!;
        public Guid WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        public string Status { get; set; } = "PENDING"; // PENDING, COMPLETED
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow; // EF sẽ tự điền giá trị

        public ICollection<GoodsIssueItem> Items { get; set; } = new List<GoodsIssueItem>();
    }
}
