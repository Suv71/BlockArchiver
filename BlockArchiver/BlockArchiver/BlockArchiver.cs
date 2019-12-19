using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlockArchiver
{
    public class BlockArchiver
    {
        private int _blockSize;
        private ConcurrentQueue<byte[]> _readBlocksQueue;
        private ConcurrentQueue<byte[]> _blocksToWriteQueue;
        private Thread _readThread;
        private Thread _writeThread;
        private Thread[] _workThreads;
        private string _inputFileName;
        private string _outputFileName;
        private bool _isCancelled;
        private ManualResetEvent _readCompletedEvent;
        private ManualResetEvent _writeCompletedEvent;

        public event EventHandler<ProgressEventArgs> Progress;

        public BlockArchiver()
        {
            _blockSize = 1024 * 1024; // 1 Мб
            _readBlocksQueue = new ConcurrentQueue<byte[]>();
            _blocksToWriteQueue = new ConcurrentQueue<byte[]>();
            _workThreads = new Thread[Environment.ProcessorCount];
            _isCancelled = false;
            _readCompletedEvent = new ManualResetEvent(false);
            _writeCompletedEvent = new ManualResetEvent(false);
        }

        public int CompressFile(string inputFileName, string outputFileName)
        {
            _inputFileName = inputFileName;
            _outputFileName = outputFileName;
            ReadOriginalBlocks();
            //_readThread = new Thread(ReadOriginalBlocks);
            //_readThread.Start();

            //for (int i = 0; i < _workThreads.Length; i++)
            //{
            //    _workThreads[i] = new Thread(CompressBlock);
            //    _workThreads[i].Start();
            //}

            CompressBlock();
            //_writeThread = new Thread(WriteBlocks);
            //_writeThread.Start();
            WriteBlocks();

            //WaitHandle.WaitAll(new WaitHandle[] {_writeCompletedEvent });
            //_writeThread.Join();

            return 1;
        }

        public int DecompressFile(string inputFileName, string outputFileName)
        {
            _inputFileName = inputFileName;
            _outputFileName = outputFileName;
            //_readThread = new Thread(ReadCompressedBlocks);
            //_readThread.Start();
            ReadCompressedBlocks();
            //for (int i = 0; i < _workThreads.Length; i++)
            //{
            //    _workThreads[i] = new Thread(DecompressBlock);
            //    _workThreads[i].Start();
            //}
            DecompressBlock();
            //_writeThread = new Thread(WriteBlocks);
            //_writeThread.Start();
            WriteBlocks();
            //WaitHandle.WaitAll(new WaitHandle[] { _writeCompletedEvent });
            //_writeThread.Join();

            return 1;
        }

        private void ReadOriginalBlocks()
        {
            using (var inputStream = File.OpenRead(_inputFileName))
            {
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
                    _readBlocksQueue.Enqueue(readBlock);
                }
                //_readCompletedEvent.Set();
            }
        }

        private void ReadCompressedBlocks()
        {
            using (var inputStream = File.OpenRead(_inputFileName))
            {
                byte[] readBlock;
                var lengthBlock = new byte[8];

                while (inputStream.Position < inputStream.Length)
                {
                    inputStream.Read(lengthBlock, 0, lengthBlock.Length);
                    var compressedBlockLength = BitConverter.ToInt32(lengthBlock, 4);

                    readBlock = new byte[compressedBlockLength];
                    lengthBlock.CopyTo(readBlock, 0);

                    inputStream.Read(readBlock, lengthBlock.Length, compressedBlockLength - lengthBlock.Length);

                    _readBlocksQueue.Enqueue(readBlock);
                }
                //_readCompletedEvent.Set();
            }
        }

        private void CompressBlock()
        {
            while (!_readBlocksQueue.IsEmpty /*|| _readThread.IsAlive*/ /*!_readCompletedEvent.WaitOne(0, false)*/)
            {
                if (_readBlocksQueue.TryDequeue(out var tempBlock))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                        {
                            gzipStream.Write(tempBlock, 0, tempBlock.Length);
                        }
                        var compressedBlock = memoryStream.ToArray();
                        var compressedBlockLengthInBytes = BitConverter.GetBytes(compressedBlock.Length);
                        compressedBlockLengthInBytes.CopyTo(compressedBlock, 4);
                        _blocksToWriteQueue.Enqueue(compressedBlock);
                    }
                }
            }
        }

        private void DecompressBlock()
        {
            while (!_readBlocksQueue.IsEmpty /*|| _readThread.IsAlive *//*!_readCompletedEvent.WaitOne(0, false)*/)
            {
                if (_readBlocksQueue.TryDequeue(out var tempBlock))
                {
                    var uncompressedBlockSize = BitConverter.ToInt32(tempBlock, tempBlock.Length - 4);
                    var uncompressedBlock = new byte[uncompressedBlockSize];

                    using (var memoryStream = new MemoryStream(tempBlock))
                    {
                        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                        {
                            gzipStream.Read(uncompressedBlock, 0, uncompressedBlockSize);
                        }

                        _blocksToWriteQueue.Enqueue(uncompressedBlock);
                    }
                }
            }
        }

        private void WriteBlocks()
        {
            while (!_blocksToWriteQueue.IsEmpty /*|| _readThread.IsAlive || _workThreads.Any(th => th.IsAlive)*/)
            {
                if (_blocksToWriteQueue.TryDequeue(out var tempBlock))
                {
                    using (var outputStream = File.Open(_outputFileName, FileMode.Append))
                    {
                        outputStream.Write(tempBlock, 0, tempBlock.Length);
                        Progress.Invoke(this, new ProgressEventArgs(1));
                    }
                }  
            }
            //_writeCompletedEvent.Set();
        }
    }

    public class ProgressEventArgs : EventArgs
    {
        public int Count { get; set; }

        public ProgressEventArgs(int count)
        {
            Count = count;
        }
    }
}
