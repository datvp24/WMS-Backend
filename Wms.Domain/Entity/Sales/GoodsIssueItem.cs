using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Warehouses;

namespace Wms.Domain.Entity.Sales
{
    public class GoodsIssueItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GoodsIssueId { get; set; }
        public GoodsIssue GoodsIssue { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public Guid LocationId { get; set; }
        public Location Location { get; set; } = null!;

        public int Quantity { get; set; }
    }
}
