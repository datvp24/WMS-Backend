using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wms.Application.DTOS.Transfer
{
    public class TransferOrderDto
    {
        public Guid Id { get; set; } // Quan trọng để làm rowKey
        public string Code { get; set; } = string.Empty;
        public Guid FromWarehouseId { get; set; }
        public string? FromWarehouseName { get; set; }
        public Guid ToWarehouseId { get; set; }
        public string? ToWarehouseName { get; set; }
        public string? Status { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TransferOrderItemDto> Items { get; set; } = new();
    }
}
