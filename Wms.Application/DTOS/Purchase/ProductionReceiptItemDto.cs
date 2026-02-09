using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wms.Domain.Entity.Purchase;
using Wms.Domain.Enums.Purchase;

namespace Wms.Application.DTOS.Purchase
{
    public class ProductionReceiptItemDto
    {
        public Guid Id { get; set; }
        public Guid GoodsReceiptId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int Receipt_Qty { get; set; }
        public string? LotCode { get; set; }
        public DateTime ExpiryDate { get; set; }
            
        public GRIStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
