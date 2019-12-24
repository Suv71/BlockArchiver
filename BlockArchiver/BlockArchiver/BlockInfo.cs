using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockArchiver
{
    public struct BlockInfo
    {
        public int Number { get; set; }
        public byte[] Data { get; set; }
    }
}
