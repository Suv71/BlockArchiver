using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockArchiver
{
    class Program
    {
        static void Main(string[] args)
        {
            var blockArchiver = new BlockArchiver();
            blockArchiver.Progress += OnProgress;
            var k = blockArchiver.CompressFile("Windows 7 Ultimate x64.iso", "Windows 7 Ultimate x64.gz");

            //var k = blockArchiver.DecompressFile("Windows 7 Ultimate x64.gz", "new-Windows 7 Ultimate x64.iso");

            Console.WriteLine($"Compressing is over. Result = {k}");

            Console.ReadKey();
        }

        static void OnProgress(object sender, ProgressEventArgs e)
        {
            Console.WriteLine();
            Console.WriteLine($"Current progress - {e.Count}");
        }
    }
}
