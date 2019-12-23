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
    public class Compressor : BlockArchiver
    {
        public Compressor(string inputFileName, string outputFileName) 
            : base(inputFileName, outputFileName)
        {
        }

        protected override void ReadBlocks()
        {
            using (var inputStream = File.OpenRead(_inputFileName))
            {
                var currentBlockNumber = 1;
                byte[] readBlock;
                while (inputStream.Position < inputStream.Length)
                {
                    if (inputStream.Length - inputStream.Position >= _blockSize)
                    {
                        readBlock = new byte[_blockSize];
                    }
                    else
                    {
                        readBlock = new byte[inputStream.Length - inputStream.Position];
                    }
                    inputStream.Read(readBlock, 0, readBlock.Length);
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
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                        {
                            gzipStream.Write(tempBlock.Data, 0, tempBlock.Data.Length);
                        }
                        var compressedBlock = memoryStream.ToArray();
                        var compressedBlockLengthInBytes = BitConverter.GetBytes(compressedBlock.Length);
                        compressedBlockLengthInBytes.CopyTo(compressedBlock, 4);
                        tempBlock.Data = compressedBlock;
                        _blocksToWriteQueue.TryAdd(tempBlock.Number, tempBlock);
                        _dispathcer.ContinueWriting();
                    }
                }
            }
        }
    }
}
