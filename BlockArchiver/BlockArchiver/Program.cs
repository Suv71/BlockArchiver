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
            //var k = blockArchiver.CompressFile("datagrip-2019.1.1.exe", "datagrip-2019.1.1.gz");

            var k = blockArchiver.DecompressFile("datagrip-2019.1.1.gz", "new-datagrip-2019.1.1.exe");

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
