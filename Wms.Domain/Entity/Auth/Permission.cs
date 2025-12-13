using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wms.Domain.Entity.Auth
{
    public class Permission : BaseEntity
    {
        public string Code { get; set; } = ""; // e.g. "product.view"
        public string Description { get; set; } = "";

        public ICollection<RolePermission>? RolePermissions { get; set; }
        public ICollection<UserPermission>? UserPermissions { get; set; }
    }

}
