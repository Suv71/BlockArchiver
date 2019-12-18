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
            blockArchiver.CompressFile("Справочник сотрудника.docx", "Справочник сотрудника.gz");

            Console.ReadKey();
        }
    }
}
