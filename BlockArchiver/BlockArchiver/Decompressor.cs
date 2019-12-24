using System;
using System.IO;
using System.IO.Compression;

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
            try
            {
                using (var inputStream = File.OpenRead(_inputFileName))
                {
                    var currentBlockNumber = 1;
                    byte[] readBlock;
                    var lengthBlock = new byte[8];

                    while (!_isError && inputStream.Position < inputStream.Length)
                    {
                        inputStream.Read(lengthBlock, 0, lengthBlock.Length);
                        var compressedBlockLength = BitConverter.ToInt32(lengthBlock, 4);

                        readBlock = new byte[compressedBlockLength];
                        lengthBlock.CopyTo(readBlock, 0);

                        inputStream.Read(readBlock, lengthBlock.Length, compressedBlockLength - lengthBlock.Length);

                        _readBlocks.Enqueue(new BlockInfo() { Number = currentBlockNumber++, Data = readBlock });

                        if (_dispathcer.IsUsedMemoryMoreLimit())
                        {
                            GC.Collect();
                            _dispathcer.PauseReading();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs($"Возникла ошибка при чтении блоков из файла: {ex.Message}"));
            }
        }

        protected override void ProcessReadBlocks()
        {
            try
            {
                while (!_isError && (!_readBlocks.IsEmpty || _dispathcer.IsReadingNotOver()))
                {
                    if (_readBlocks.TryDequeue(out var tempBlock))
                    {
                        var uncompressedBlockLength = BitConverter.ToInt32(tempBlock.Data, tempBlock.Data.Length - _intBlockLength);
                        var uncompressedBlock = new byte[uncompressedBlockLength];

                        using (var memoryStream = new MemoryStream(tempBlock.Data))
                        {
                            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                            {
                                gzipStream.Read(uncompressedBlock, 0, uncompressedBlockLength);
                            }
                            tempBlock.Data = uncompressedBlock;
                            _blocksToWrite.TryAdd(tempBlock.Number, tempBlock);
                            _dispathcer.ContinueWriting();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs($"Возникла ошибка при чтении блоков из файла: {ex.Message}"));
            } 
        }
    }
}
