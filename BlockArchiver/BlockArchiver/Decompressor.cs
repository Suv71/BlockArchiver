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
                    var lengthBlock = new byte[_int64BlockLength];
                    inputStream.Read(lengthBlock, 0, lengthBlock.Length);
                    _uncompressedFileLength = BitConverter.ToInt64(lengthBlock, 0);
                    SetTotalBlockNumber();
                    var currentBlockNumber = 1;
                    byte[] readBlock;

                    while (!_isCancelled && inputStream.Position < inputStream.Length)
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
                OnError(new ErrorEventArgs($"Возникла ошибка при чтении блоков из файла: {ex.Message}", ex));
            }
        }

        protected override void ProcessReadBlocks()
        {
            try
            {
                while (!_isCancelled && (!_readBlocks.IsEmpty || _dispathcer.IsReadingNotOver()))
                {
                    if (_readBlocks.TryDequeue(out var blockInfo))
                    {
                        var uncompressedBlockLength = BitConverter.ToInt32(blockInfo.Data, blockInfo.Data.Length - _int32BlockLength);
                        var uncompressedBlock = new byte[uncompressedBlockLength];

                        using (var memoryStream = new MemoryStream(blockInfo.Data))
                        {
                            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                            {
                                gzipStream.Read(uncompressedBlock, 0, uncompressedBlockLength);
                            }
                            blockInfo.Data = uncompressedBlock;
                            _blocksToWrite.TryAdd(blockInfo.Number, blockInfo);
                            _dispathcer.ContinueWriting();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs($"Возникла ошибка при чтении блоков из файла: {ex.Message}", ex));
            } 
        }
    }
}
