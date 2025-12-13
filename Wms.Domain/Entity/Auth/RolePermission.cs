using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wms.Domain.Entity.Auth
{
    public class RolePermission
    {
        [Key]
        public int RoleId { get; set; }
        public Role? Role { get; set; }

        public int PermissionId { get; set; }
        public Permission? Permission { get; set; }
    }

}
