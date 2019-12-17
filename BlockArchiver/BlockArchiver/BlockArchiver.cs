using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockArchiver
{
    public class BlockArchiver
    {
        public (List<int>, List<int>) CompressFile(string filePath)
        {
            var lst1 = new List<int>();
            var lst2 = new List<int>();
            //var temp = new byte[1 * 1024 * 1024];

            using (var inputStream = File.OpenRead(filePath))
            {
                using (var outputStream = File.Open(filePath + ".gz", FileMode.Append))
                {
                    var readBytes = 0;
                    
                    while (inputStream.Position < inputStream.Length)
                    {
                        if (inputStream.Length - inputStream.Position <= 1024 * 1024)
                        {
                            readBytes = (int)(inputStream.Length - inputStream.Position);
                        }
                        else
                        {
                            readBytes = 1024 * 1024;
                        }
                        var temp = new byte[readBytes];

                        inputStream.Read(temp, 0, readBytes);

                        using (var mem = new MemoryStream())
                        {
                            using (var compressStream = new GZipStream(mem, CompressionMode.Compress))
                            {
                                compressStream.Write(temp, 0, readBytes);
                                var arrToWrite = mem.ToArray();
                                outputStream.Write(arrToWrite, 0, arrToWrite.Length);
                                var k = BitConverter.ToInt32(arrToWrite, arrToWrite.Length - 4);
                                lst1.Add(arrToWrite.Length);
                                lst2.Add(readBytes);
                            }
                        }   
                    }
                }  
            }
            return (lst1, lst2);
        }

        public void DecompressFile(string filePath, List<int> lst1, List<int> lst2)
        {
            using (var inputStream = File.OpenRead(filePath))
            {
                using (var outputStream = File.Open($"new{filePath.Replace(".gz", "")}", FileMode.Append))
                {
                    var i = 0;
                    while (i < lst1.Count)
                    {
                        var compressedData = new byte[lst1[i]];
                        inputStream.Read(compressedData, 0, lst1[i]);

                        var m = BitConverter.ToInt32(compressedData, lst1[i] - 4);

                        using (var mem = new MemoryStream(compressedData))
                        {
                            using (var decompressStream = new GZipStream(mem, CompressionMode.Decompress))
                            {
                                var uncomp = new byte[lst2[i]];
                                decompressStream.Read(uncomp, 0, uncomp.Length);
                                outputStream.Write(uncomp, 0, uncomp.Length);
                                i++;
                            }
                        }
                        
                    }   
                }
            }
        }
    }
}
