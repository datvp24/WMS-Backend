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
        public SOStatus Status { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? ApproveBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
        public ICollection<GoodsIssue> GoodsIssues { get; set; } = new List<GoodsIssue>();
    }
    public enum SOStatus
    {
        Pending = 0,
        Approve = 1,
        Partically_Issued = 2,
        Complete = 3,
        Rejected = 4,
    }
}
