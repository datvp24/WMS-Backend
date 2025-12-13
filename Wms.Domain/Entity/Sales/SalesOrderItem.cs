using Wms.Domain.Entity.MasterData;

namespace Wms.Domain.Entity.Sales
{
    public class SalesOrderItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SalesOrderId { get; set; }
        public SalesOrder SalesOrder { get; set; } = null!;

        public int ProductId { get; set; }
        public   Product Product { get; set; } = null!;

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
