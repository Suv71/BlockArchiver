using System;
using System.IO;

namespace BlockArchiver
{
    class Program
    {
        static int Main(string[] args)
        {
            var result = 0;
            try
            {
                CheckArgrs(args);
                BlockArchiver archiver;

                if (args[0].Equals("compress"))
                {
                    archiver = new Compressor(args[1], args[2]);
                }
                else
                {
                    archiver = new Decompressor(args[1], args[2]);
                }

                archiver.Progress += OnProgress;
                archiver.Error += OnError;

                result = archiver.Do();
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                result = 1;
            }

            return result;
        }

        static void CheckArgrs(string[] args)
        {
            if (args.Length != 3)
            {
                throw new ArgumentException("Неверное количество аргументов");
            }

            if (!args[0].Equals("compress") && !args[0].Equals("decompress"))
            {
                throw new ArgumentException("Неверно указано операция");
            }

            if (!File.Exists(args[1]))
            {
                throw new ArgumentException("Входного файла не существует");
            }

            if (!new FileInfo(args[1]).Extension.Equals("gz"))
            {
                throw new ArgumentException("Входной файл должен иметь расширение .gz");
            }
        }

        static void OnProgress(object sender, ProgressEventArgs e)
        {
            Console.CursorLeft = 0;
            Console.Write($"Выполнение - {e.Progress}%");
        }

        static void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
