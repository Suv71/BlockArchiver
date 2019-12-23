using System;

namespace BlockArchiver
{
    class Program
    {
        static void Main(string[] args)
        {
            var blockArchiver = new BlockArchiver();
            blockArchiver.Progress += OnProgress;
            var k = blockArchiver.CompressFile("Fias.backup", "Fias.gz");

            //var k = blockArchiver.DecompressFile("Fias.gz", "new-Fias.backup");

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
