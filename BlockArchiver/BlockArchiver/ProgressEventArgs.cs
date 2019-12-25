using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockArchiver
{
    public class ProgressEventArgs : EventArgs
    {
        public int Progress { get; set; }

        public ProgressEventArgs(long current, long total)
        {
            Progress = (int)(current * 100 / total);
        }
    }
}
