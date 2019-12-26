using System;

namespace BlockArchiver
{
    public class ProgressEventArgs : EventArgs
    {
        public int Progress { get; set; }

        public ProgressEventArgs(int current, int total)
        {
            Progress = current * 100 / total;
        }
    }
}
