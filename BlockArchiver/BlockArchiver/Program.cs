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
            //Console.WriteLine(Environment.ProcessorCount);
            //Console.WriteLine(Environment.SystemPageSize);

            var blockArchiver = new BlockArchiver();
            //var k = blockArchiver.CompressFile(@"Справочник сотрудника.docx");
            //blockArchiver.DecompressFile(@"Справочник сотрудника.docx.gz", k.Item1, k.Item2);

            Console.ReadKey();
        }
    }
}
