using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockArchiver
{
    public class Decompressor : BlockArchiver
    {
        public Decompressor(string inputFileName, string outputFileName)
            : base(inputFileName, outputFileName)
        {
        }

        protected override void ReadBlocks()
        {
            using (var inputStream = File.OpenRead(_inputFileName))
            {
                var currentBlockNumber = 1;
                byte[] readBlock;
                var lengthBlock = new byte[8];

                while (inputStream.Position < inputStream.Length)
                {
                    inputStream.Read(lengthBlock, 0, lengthBlock.Length);
                    var compressedBlockLength = BitConverter.ToInt32(lengthBlock, 4);

                    readBlock = new byte[compressedBlockLength];
                    lengthBlock.CopyTo(readBlock, 0);

                    inputStream.Read(readBlock, lengthBlock.Length, compressedBlockLength - lengthBlock.Length);

                    _readBlocksQueue.Enqueue(new BlockInfo() { Number = currentBlockNumber++, Data = readBlock });

                    if (Process.GetCurrentProcess().WorkingSet64 > _memoryLimit)
                    {
                        _dispathcer.PauseReading();
                    }
                }
            }
        }

        protected override void ProcessReadBlocks()
        {
            while (!_readBlocksQueue.IsEmpty || _dispathcer.IsReadingNotOver())
            {
                if (_readBlocksQueue.TryDequeue(out var tempBlock))
                {
                    var uncompressedBlockSize = BitConverter.ToInt32(tempBlock.Data, tempBlock.Data.Length - 4);
                    var uncompressedBlock = new byte[uncompressedBlockSize];

                    using (var memoryStream = new MemoryStream(tempBlock.Data))
                    {
                        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                        {
                            gzipStream.Read(uncompressedBlock, 0, uncompressedBlockSize);
                        }
                        tempBlock.Data = uncompressedBlock;
                        _blocksToWriteQueue.TryAdd(tempBlock.Number, tempBlock);
                        _dispathcer.ContinueWriting();
                    }
                }
            }
        }
    }
}
