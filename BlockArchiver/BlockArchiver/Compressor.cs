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
                using (var inputStream = File.OpenRead(_inputFileName))
                {
                    _uncompressedFileLength = inputStream.Length;
                    var currentBlockNumber = 1;
                    byte[] readBlock;
                    while (!_isError && inputStream.Position < inputStream.Length)
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
                while (!_isError && (!_readBlocks.IsEmpty || _dispathcer.IsReadingNotOver()))
                {
                    if (_readBlocks.TryDequeue(out var tempBlock))
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
                            _blocksToWrite.TryAdd(tempBlock.Number, tempBlock);
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
            var tempBlock = BitConverter.GetBytes(_uncompressedFileLength);
            outputStream.Write(tempBlock, 0, tempBlock.Length);
        }
    }
}
