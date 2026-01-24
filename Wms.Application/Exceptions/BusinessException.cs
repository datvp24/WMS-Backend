using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wms.Application.Exceptions
{
    public class BusinessException : Exception
    {
        public string Code { get; }

        public BusinessException(string code, string message) : base(message)
        {
            Code = code;
        }
    }

}
