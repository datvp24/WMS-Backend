using System;
using System.Collections.Generic;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Auth;

namespace Wms.Domain.Entity.Sales
{
    public class SalesOrder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = null!;
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public int CreatedBy { get; set; }
        public string Status { get; set; } = "DRAFT"; // DRAFT, PENDING, APPROVED, REJECTED
        public bool LockedStock { get; set; } = false;
        public decimal TotalAmount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
        public ICollection<GoodsIssue> GoodsIssues { get; set; } = new List<GoodsIssue>();
    }
}
