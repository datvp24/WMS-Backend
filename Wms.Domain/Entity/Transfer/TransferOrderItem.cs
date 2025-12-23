using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Entity.MasterData;

namespace Wms.Domain.Entity.Transfer
{
    public class TransferOrderItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TransferOrderId { get; set; }

        [ForeignKey(nameof(TransferOrderId))]
        public TransferOrder TransferOrder { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; }

        // From
        [Required]
        public Guid FromLocationId { get; set; }

        [ForeignKey(nameof(FromLocationId))]
        public Location FromLocation { get; set; }

        // To
        [Required]
        public Guid ToLocationId { get; set; }

        [ForeignKey(nameof(ToLocationId))]
        public Location ToLocation { get; set; }

        [Required]
        public decimal Quantity { get; set; }

        [MaxLength(200)]
        public string Note { get; set; }
    }
}

