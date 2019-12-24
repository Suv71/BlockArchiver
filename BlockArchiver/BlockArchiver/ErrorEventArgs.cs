using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockArchiver
{
    public class ErrorEventArgs
    {
        public string Message { get; set; }

        public ErrorEventArgs(string message)
        {
            Message = message;
        }
    }
}
