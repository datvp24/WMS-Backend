using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wms.Application.DTOS.Auth
{
    public class UpdateUserDto
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Password { get; set; }
        public bool IsActive { get; set; }

    }

}
