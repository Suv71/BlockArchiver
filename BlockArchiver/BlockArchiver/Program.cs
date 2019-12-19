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
            //var k = blockArchiver.CompressFile("Справочник сотрудника.docx", "Справочник сотрудника.gz");

            var k = blockArchiver.DecompressFile("Справочник сотрудника.gz", "new-Справочник сотрудника.docx");

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
