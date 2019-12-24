using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockArchiver
{
    public class ProgressEventArgs : EventArgs
    {
        public int Count { get; set; }

        public ProgressEventArgs(int count)
        {
            Count = count;
        }
    }
}
