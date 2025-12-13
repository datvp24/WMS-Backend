using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wms.Domain.Entity.Warehouses
{
    public enum WarehouseStatus
    {
        Active = 1,
        Inactive = 2,
        Locked = 3,
        Maintenance = 4
    }


    public class Warehouse
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();


        [Required, MaxLength(100)]
        public string Code { get; set; }


        [Required, MaxLength(200)]
        public string Name { get; set; }


        public string Address { get; set; }


        public WarehouseStatus Status { get; set; } = WarehouseStatus.Active;


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        public DateTime? UpdatedAt { get; set; }


        // Navigation
        public ICollection<Location> Locations { get; set; }
    }
}
