using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Wms.Domain.Enums.location;
using System.Text;
using System.Threading.Tasks;

namespace Wms.Application.DTOS.Warehouse
{
    public class LocationCreateDto
    {
        [Required]
        public Guid WarehouseId { get; set; }


        [Required, MaxLength(50)]
        public string Code { get; set; }

        public LocationType Type {  get; set; }
        public string Description { get; set; }
    }


    public class LocationUpdateDto
    {
        [Required]
        public Guid Id { get; set; }


        public string Description { get; set; }
        public LocationType LocationType { get; set; }
        public bool? IsActive { get; set; }
        public string? Code { get; set; }
    }


    public class LocationDto
    {
        public Guid Id { get; set; }
        public Guid WarehouseId { get; set; }
        public LocationType Type { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class LocationQtyDto
    {
        public Guid Id { get; set; }
        public Guid WarehouseId { get; set; }
        public LocationType Type { get; set; }
        public string Code { get; set; }
        public decimal AvailableQty {  get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
