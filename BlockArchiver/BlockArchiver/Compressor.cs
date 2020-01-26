using System;
using System.IO;
using System.IO.Compression;

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
            try
            {
                using (var inputStream = new FileStream(_inputFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    _uncompressedFileLength = inputStream.Length;
                    SetTotalBlockNumber();
                    var currentBlockNumber = 1;
                    byte[] readBlock;
                    while (!_isCancelled && inputStream.Position < inputStream.Length)
                    {
                        if (inputStream.Length - inputStream.Position >= _blockLength)
                        {
                            readBlock = new byte[_blockLength];
                        }
                        else
                        {
                            readBlock = new byte[inputStream.Length - inputStream.Position];
                        }
                        inputStream.Read(readBlock, 0, readBlock.Length);
                        _readBlocks.Enqueue(new BlockInfo() { Number = currentBlockNumber++, Data = readBlock });

                        if (_dispathcer.IsUsedMemoryMoreLimit())
                        {
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
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                            {
                                gzipStream.Write(blockInfo.Data, 0, blockInfo.Data.Length);
                            }
                            var compressedBlock = memoryStream.ToArray();
                            var compressedBlockLengthBytes = BitConverter.GetBytes(compressedBlock.Length);
                            compressedBlockLengthBytes.CopyTo(compressedBlock, 4);
                            blockInfo.Data = compressedBlock;
                            _blocksToWrite.TryAdd(blockInfo.Number, blockInfo);
                            _dispathcer.ContinueWriting();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs($"Возникла ошибка при обработке блоков: {ex.Message}", ex));
            }
        }

        protected override void WriteUncompressedFileLength(FileStream outputStream)
        {
            var uncompressedFileLengthBytes = BitConverter.GetBytes(_uncompressedFileLength);
            outputStream.Write(uncompressedFileLengthBytes, 0, uncompressedFileLengthBytes.Length);
        }
    }
}
