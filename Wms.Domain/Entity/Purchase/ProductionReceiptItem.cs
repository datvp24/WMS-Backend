using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wms.Domain.Enums.Purchase;

namespace Wms.Domain.Entity.Purchase
{
    public class ProductionReceiptItem
    {
        public Guid Id {  get; set; }
        public Guid GoodsReceiptId { get; set; }
        public int ProductId {  get; set; }
        public int Quantity { get; set; }
        public int Receipt_Qty { get; set; }
        public GRIStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set;}
        public GoodsReceipt GoodsReceipt { get; set; }

    }
}
