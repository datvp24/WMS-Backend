using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wms.Domain.Entity.Auth
{
    public class User : BaseEntity
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public bool IsActive { get; set; } = true;

        public ICollection<UserRole>? UserRoles { get; set; }
        public ICollection<UserPermission>? UserPermissions { get; set; }
    }

}
