using System;

namespace BlockArchiver
{
    class Program
    {
        static void Main(string[] args)
        {
            BlockArchiver archiver;

            //archiver = new Compressor("Fias.backup", "Fias.gz");
            archiver = new Decompressor("Fias.gz", "new-Fias.backup");

            archiver.Progress += OnProgress;

            var k = archiver.Do();

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
