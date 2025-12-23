using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wms.Application.DTOS.Transfer
{
    public class TransferOrderItemDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public Guid FromLocationId { get; set; }
        public string? FromLocationCode { get; set; }
        public Guid ToLocationId { get; set; }
        public string? ToLocationCode { get; set; }
        public decimal Quantity { get; set; }
        public string? Note { get; set; }
    }
}
