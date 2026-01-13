using System.ComponentModel.DataAnnotations;
using Wms.Domain.Entity.Warehouses;
using Wms.Application.DTOS.Warehouse;

namespace Wms.Application.DTOS.Warehouse
{
    public class WarehouseCreateDto
    {
        [Required, MaxLength(100)]
        public string Code { get; set; }


        [Required, MaxLength(200)]
        public string Name { get; set; }


        public string Address { get; set; }
    }

    public class WarehousesbyProduct
    {
        public int ProductId {  get; set; }
    }
    public class WarehouseUpdateDto
    {
        [Required]
        public Guid Id { get; set; }


        [Required, MaxLength(200)]
        public string Name { get; set; }

        public string? Code { get; set; }
        public string Address { get; set; }


        public WarehouseStatus? Status { get; set; }
    }


    public class WarehouseDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public WarehouseStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<LocationDto> Locations { get; set; }
    }
}