using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wms.Domain.Entity.Inventorys
{
    public class Lot
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public int productId { get; set; }
        public DateTime? ExpiryDate { get; set; } 
        public DateTime? ManufacturingDate { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
