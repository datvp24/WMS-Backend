using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Enums.Transfer;

namespace Wms.Domain.Entity.Transfer
{
    public class TransferOrder
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(50)]
        public string Code { get; set; }   // TRF-2025-0001

        [Required]
        public Guid FromWarehouseId { get; set; }

        [Required]
        public Guid ToWarehouseId { get; set; }

        [ForeignKey(nameof(FromWarehouseId))]
        public Warehouse FromWarehouse { get; set; }

        [ForeignKey(nameof(ToWarehouseId))]
        public Warehouse ToWarehouse { get; set; }

        public TransferStatus Status { get; set; } = TransferStatus.Draft;

        [MaxLength(500)]
        public string Note { get; set; }

        // Approve
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Audit
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<TransferOrderItem> Items { get; set; }
    }
}
